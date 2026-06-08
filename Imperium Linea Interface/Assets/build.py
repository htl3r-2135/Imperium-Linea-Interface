import argparse
import hashlib
import hmac
import json
import logging
import os
import platform
import subprocess
import sys
import tarfile
import tempfile
import time
from pathlib import Path

import requests


# ---------------------------------------------------------------------------
# Dynamic project root detection
# ---------------------------------------------------------------------------

def _detect_project_root() -> str:
	env_override = os.getenv("UNITY_PROJECT_PATH")
	if env_override:
		return env_override

	script_dir = Path(__file__).resolve().parent
	project_root = script_dir.parent

	if not (project_root / "ProjectSettings").exists():
		raise RuntimeError(
			f"Could not find a Unity project at '{project_root}'.\n"
			"Expected 'ProjectSettings/' to exist there.\n"
			"Move this script into <YourProject>/Assets/ or set UNITY_PROJECT_PATH."
		)

	return str(project_root)


# ---------------------------------------------------------------------------
# CONFIG
# ---------------------------------------------------------------------------

CONFIG = {
	"PROJECT_PATH": _detect_project_root(),
	"PRODUCT_NAME": os.getenv("UNITY_PRODUCT_NAME", "Imperium Linea Interface"),
	"VERSION": os.getenv("UNITY_BUILD_VERSION", "0.1.0"),
	"UNITY_VERSION": os.getenv("UNITY_VERSION", "6000.3.8f1"),

	"UNITY_HUB_PATH": os.getenv("UNITY_HUB_PATH", "unity-hub"),

	"BUILD_ROOT": os.getenv(
		"BUILD_ROOT",
		str(Path(_detect_project_root()).parent / "builds")
	),

	"NSIS_PATH": os.getenv(
		"NSIS_PATH",
		r"C:\Program Files (x86)\NSIS\makensis.exe"
	),

	"INSTALLER_ICON": os.getenv("INSTALLER_ICON", ""),

	"UPLOAD_URL": os.getenv(
		"UPLOAD_URL",
		"https://localhost/api/builds"
	),

	"HMAC_SECRET": os.getenv("HMAC_SECRET", "d504b6c8b58cc6519f30143bbf0497c08a72a2ceb5f1054aedd295feba525aa2"),
	"HMAC_HEADER": os.getenv(
		"HMAC_HEADER",
		"X-Signature-HMAC-SHA256"
	),

	"LOG_LEVEL": os.getenv("LOG_LEVEL", "INFO"),
}


# ---------------------------------------------------------------------------
# Logging
# ---------------------------------------------------------------------------

logging.basicConfig(
	level=getattr(logging, CONFIG["LOG_LEVEL"].upper(), logging.INFO),
	format="%(asctime)s  %(levelname)-8s  %(message)s",
	datefmt="%H:%M:%S",
)

log = logging.getLogger("unity-build")


# ---------------------------------------------------------------------------
# .env loader
# ---------------------------------------------------------------------------

def _load_dotenv():
	env_path = Path(__file__).resolve().parent / ".env"

	if not env_path.exists():
		return

	with open(env_path, encoding="utf-8") as f:
		for line in f:
			line = line.strip()

			if not line or line.startswith("#") or "=" not in line:
				continue

			key, _, value = line.partition("=")

			key = key.strip()
			value = value.strip().strip('"').strip("'")

			if key and key not in os.environ:
				os.environ[key] = value
				CONFIG[key] = value


_load_dotenv()


def _require_secret(key: str):
	val = CONFIG.get(key) or os.getenv(key, "")

	if not val:
		log.error(
			"Missing required secret: %s\n"
			"Set it as an environment variable or add it to Assets/.env:\n"
			"  echo %s=your-secret >> Assets\\.env",
			key,
			key
		)
		sys.exit(1)

	CONFIG[key] = val


# ---------------------------------------------------------------------------
# CLI args
# ---------------------------------------------------------------------------

def parse_args() -> argparse.Namespace:
	parser = argparse.ArgumentParser(
		description="Unity production build pipeline"
	)

	parser.add_argument(
		"--skip-build",
		action="store_true",
		help="Skip Unity builds."
	)

	parser.add_argument(
		"--skip-upload",
		action="store_true",
		help="Skip uploads."
	)

	parser.add_argument(
		"--skip-installer",
		action="store_true",
		help="Skip NSIS installer AND Linux tar.gz archive."
	)

	return parser.parse_args()


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def run(
		cmd: list[str],
		log_file: Path | None = None,
		**kwargs
) -> subprocess.CompletedProcess:

	log.info(
		"$ %s",
		" ".join(
			f'"{c}"' if " " in str(c) else str(c)
			for c in cmd
		)
	)

	try:
		result = subprocess.run(
			cmd,
			check=True,
			**kwargs
		)

		return result

	except subprocess.CalledProcessError as exc:
		log.error("Command failed with exit code %s", exc.returncode)

		if log_file and Path(log_file).exists():
			log.error("─── Unity log: %s ───", log_file)

			try:
				print(
					Path(log_file).read_text(
						encoding="utf-8",
						errors="replace"
					),
					flush=True
				)
			except Exception:
				pass

		raise


def hmac_sign_file(path: Path, secret: str) -> str:
	mac = hmac.new(
		secret.encode(),
		digestmod=hashlib.sha256
	)

	with open(path, "rb") as fh:
		for chunk in iter(lambda: fh.read(1 << 20), b""):
			mac.update(chunk)

	return mac.hexdigest()


def normalize_windows_version(version: str) -> str:
	parts = version.split(".")

	cleaned = []

	for part in parts:
		num = "".join(c for c in part if c.isdigit())

		if not num:
			num = "0"

		cleaned.append(num)

	while len(cleaned) < 4:
		cleaned.append("0")

	return ".".join(cleaned[:4])


def find_unity_executable(
		hub_path: str,
		unity_version: str
) -> Path:

	try:
		result = subprocess.run(
			[
				hub_path,
				"--",
				"--headless",
				"editors",
				"--installed"
			],
			capture_output=True,
			text=True,
			check=True,
		)

		for line in result.stdout.splitlines():
			if unity_version in line:
				parts = line.split(",")

				for part in parts:
					if "installed at" in part.lower():
						editor_dir = (
							part.split("installed at")[-1]
							.strip()
						)

						if platform.system() == "Windows":
							return (
									Path(editor_dir)
									/ "Editor"
									/ "Unity.exe"
							)

						elif platform.system() == "Darwin":
							return (
									Path(editor_dir)
									/ "Unity.app"
									/ "Contents"
									/ "MacOS"
									/ "Unity"
							)

						else:
							return (
									Path(editor_dir)
									/ "Editor"
									/ "Unity"
							)

	except Exception as exc:
		log.warning(
			"Could not query Unity Hub: %s",
			exc
		)

	candidates = [
		Path(
			f"/opt/unity/editors/{unity_version}/Editor/Unity"
		),

		Path(
			f"C:/Program Files/Unity/Hub/Editor/{unity_version}/Editor/Unity.exe"
		),

		Path(
			f"/Applications/Unity/Hub/Editor/{unity_version}/Unity.app/Contents/MacOS/Unity"
		),
	]

	for c in candidates:
		if c.exists():
			return c

	raise FileNotFoundError(
		f"Unity {unity_version} editor not found."
	)


# ---------------------------------------------------------------------------
# Build steps
# ---------------------------------------------------------------------------

def build_windows(
		unity_exe: Path,
		project: Path,
		out_dir: Path,
		product: str
) -> Path:

	exe_path = out_dir / f"{product}.exe"

	out_dir.mkdir(
		parents=True,
		exist_ok=True
	)

	log_file = out_dir / "unity_win.log"

	run([
		str(unity_exe),
		"-executeMethod", "BuildScript.BuildWindows",
		"-batchmode",
		"-nographics",
		"-quit",
		"-projectPath", str(project),
		"-buildTarget", "StandaloneWindows64",
		"-buildOutput", str(exe_path),
		"-logFile", str(log_file),
	], log_file=log_file)

	log.info("Windows build complete → %s", exe_path)

	return exe_path


def build_linux(
		unity_exe: Path,
		project: Path,
		out_dir: Path,
		product: str
) -> Path:

	bin_path = out_dir / product

	out_dir.mkdir(
		parents=True,
		exist_ok=True
	)

	log_file = out_dir / "unity_linux.log"

	run([
		str(unity_exe),
		"-batchmode",
		"-nographics",
		"-quit",
		"-projectPath", str(project),
		"-buildTarget", "StandaloneLinux64",
		"-executeMethod", "BuildScript.BuildLinux",
		"-buildOutput", str(bin_path),
		"-logFile", str(log_file),
	], log_file=log_file)

	log.info("Linux build complete → %s", bin_path)

	return bin_path


def make_nsis_installer(
		build_dir: Path,
		exe_path: Path,
		product: str,
		version: str,
		icon_path: str,
		out_dir: Path,
		nsis_bin: str,
) -> Path:

	version_fixed = normalize_windows_version(version)

	installer_path = (
			out_dir
			/ f"{product}-{version}-setup.exe"
	)

	icon_line = ""

	if icon_path and Path(icon_path).exists():
		icon_line = f'!define MUI_ICON "{icon_path}"'

	data_dir = build_dir / f"{product}_Data"

	nsis_script = f"""
!include "MUI2.nsh"
!include "FileFunc.nsh"

Name "{product} {version}"
OutFile "{installer_path}"

InstallDir "$PROGRAMFILES64\\{product}"
InstallDirRegKey HKLM "Software\\{product}" "InstallDir"

RequestExecutionLevel admin
SetCompressor /SOLID lzma
CRCCheck on
Unicode true

VIProductVersion "{version_fixed}"

VIAddVersionKey "ProductName" "{product}"
VIAddVersionKey "ProductVersion" "{version}"
VIAddVersionKey "FileVersion" "{version}"
VIAddVersionKey "FileDescription" "{product} Installer"
VIAddVersionKey "LegalCopyright" "(c) 2026"

{icon_line}

!define MUI_ABORTWARNING

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

Section "!{product}" SecCore
    SectionIn RO

    SetOutPath "$INSTDIR"

    ; Main executable + root files
    File /r "{build_dir}\\*.*"

    WriteUninstaller "$INSTDIR\\Uninstall.exe"

    WriteRegStr HKLM "Software\\{product}" "InstallDir" "$INSTDIR"

    WriteRegStr HKLM "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{product}" "DisplayName" "{product}"

    WriteRegStr HKLM "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{product}" "DisplayVersion" "{version}"

    WriteRegStr HKLM "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{product}" "Publisher" "YourStudio"

    WriteRegStr HKLM "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{product}" "InstallLocation" "$INSTDIR"

    WriteRegStr HKLM "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{product}" "UninstallString" "$INSTDIR\\Uninstall.exe"

    WriteRegStr HKLM "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{product}" "DisplayIcon" "$INSTDIR\\{exe_path.name}"

    WriteRegDWORD HKLM "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{product}" "NoModify" 1

    WriteRegDWORD HKLM "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{product}" "NoRepair" 1
SectionEnd


Section "Desktop Shortcut" SecDesktop
	CreateShortcut "$DESKTOP\\{product}.lnk" "$INSTDIR\\{exe_path.name}"
SectionEnd


Section "Start Menu Shortcuts" SecStartMenu
	CreateDirectory "$SMPROGRAMS\\{product}"

	CreateShortcut "$SMPROGRAMS\\{product}\\{product}.lnk" "$INSTDIR\\{exe_path.name}"

	CreateShortcut "$SMPROGRAMS\\{product}\\Uninstall.lnk" "$INSTDIR\\Uninstall.exe"
SectionEnd


Section "Uninstall"
	RMDir /r "$INSTDIR"

	Delete "$DESKTOP\\{product}.lnk"

	RMDir /r "$SMPROGRAMS\\{product}"

	DeleteRegKey HKLM "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{product}"

	DeleteRegKey HKLM "Software\\{product}"
SectionEnd
"""

	with tempfile.NamedTemporaryFile(
			mode="w",
			suffix=".nsi",
			delete=False,
			encoding="utf-8"
	) as nsi_file:

		nsi_file.write(nsis_script)

		nsi_path = nsi_file.name

	try:
		run([
			nsis_bin,
			"/V4",
			nsi_path
		])

	finally:
		os.unlink(nsi_path)

	log.info(
		"NSIS installer → %s",
		installer_path
	)

	return installer_path


def make_linux_archive(
		build_dir: Path,
		product: str,
		version: str,
		out_dir: Path
) -> Path:

	archive_path = (
			out_dir
			/ f"{product}-{version}-linux-x64.tar.gz"
	)

	with tarfile.open(
			archive_path,
			"w:gz"
	) as tar:

		tar.add(
			build_dir,
			arcname=f"{product}-{version}"
		)

	log.info("Linux archive → %s", archive_path)

	return archive_path


# ---------------------------------------------------------------------------
# Upload
# ---------------------------------------------------------------------------

def upload_artifact(
		artifact: Path,
		secret: str,
		upload_url: str,
		hmac_header: str,
		metadata: dict
):

	signature = hmac_sign_file(
		artifact,
		secret
	)

	sha256 = hashlib.sha256(
		artifact.read_bytes()
	).hexdigest()

	meta = {
		**metadata,
		"filename": artifact.name,
		"sha256": sha256,
		"timestamp": int(time.time())
	}

	log.info(
		"Uploading %s (%.1f MB)…",
		artifact.name,
		artifact.stat().st_size / 1e6
	)

	with open(artifact, "rb") as fh:
		response = requests.post(
			upload_url,
			headers={
				hmac_header: signature
			},
			files={
				"file": (
					artifact.name,
					fh,
					"application/octet-stream"
				)
			},
			data={
				"metadata": json.dumps(meta)
			},
			timeout=600,
			verify=False,
		)

	if response.ok:
		log.info(
			"✓ Upload successful [%s] %s",
			response.status_code,
			artifact.name
		)
	else:
		log.error(
			"✗ Upload failed [%s] %s\n%s",
			response.status_code,
			artifact.name,
			response.text
		)

		response.raise_for_status()

	return response


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main():
	args = parse_args()
	cfg = CONFIG

	if not args.skip_upload:
		_require_secret("HMAC_SECRET")

	project = Path(cfg["PROJECT_PATH"]).resolve()

	build_root = Path(cfg["BUILD_ROOT"]).resolve()

	product = cfg["PRODUCT_NAME"]
	version = cfg["VERSION"]

	win_build_dir = build_root / "win64"
	linux_build_dir = build_root / "linux64"

	dist_dir = build_root / "dist"

	dist_dir.mkdir(
		parents=True,
		exist_ok=True
	)

	if args.skip_build:
		win_exe = win_build_dir / f"{product}.exe"
		linux_bin = linux_build_dir / product
	else:
		unity_exe = find_unity_executable(
			cfg["UNITY_HUB_PATH"],
			cfg["UNITY_VERSION"]
		)

		win_exe = build_windows(
			unity_exe,
			project,
			win_build_dir,
			product
		)

		linux_bin = build_linux(
			unity_exe,
			project,
			linux_build_dir,
			product
		)

	installer = dist_dir / f"{product}-{version}-setup.exe"
	archive = dist_dir / f"{product}-{version}-linux-x64.tar.gz"

	if not args.skip_installer:
		installer = make_nsis_installer(
			build_dir=win_build_dir,
			exe_path=win_exe,
			product=product,
			version=version,
			icon_path=cfg["INSTALLER_ICON"],
			out_dir=dist_dir,
			nsis_bin=cfg["NSIS_PATH"],
		)

		archive = make_linux_archive(
			linux_build_dir,
			product,
			version,
			dist_dir
		)
	else:
		if not installer.exists():
			log.error(
				"--skip-installer set but installer missing: %s",
				installer
			)
			sys.exit(1)

		if not archive.exists():
			log.error(
				"--skip-installer set but archive missing: %s",
				archive
			)
			sys.exit(1)

		log.info(
			"Reusing existing installer: %s",
			installer
		)

		log.info(
			"Reusing existing archive: %s",
			archive
		)

	if not args.skip_upload:
		base_meta = {
			"product": product,
			"version": version
		}

		if installer:
			upload_artifact(
				artifact=installer,
				secret=cfg["HMAC_SECRET"],
				upload_url=cfg["UPLOAD_URL"],
				hmac_header=cfg["HMAC_HEADER"],
				metadata={
					**base_meta,
					"platform": "windows"
				},
			)

		if archive:
			upload_artifact(
				artifact=archive,
				secret=cfg["HMAC_SECRET"],
				upload_url=cfg["UPLOAD_URL"],
				hmac_header=cfg["HMAC_HEADER"],
				metadata={
					**base_meta,
					"platform": "linux"
				},
			)

	log.info("Done.")


if __name__ == "__main__":
	main()