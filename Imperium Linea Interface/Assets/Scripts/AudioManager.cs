using System.Collections;
using Abstract;
using UnityEngine;

public class AudioManager : MonoSingleton<AudioManager>
{
    private const float TransitionOffset = 0.05f;

    [Header("Doors")] [SerializeField] private AudioSource doorLeft;

    [SerializeField] private AudioSource doorRight;

    [Header("Computer")] [SerializeField] private AudioSource keyPressSource;

    [SerializeField] private AudioClip keyPressClip;


    [Header("Rotation")] [SerializeField] private AudioSource rotation;

    [SerializeField] private AudioClip rotationIntro;
    [SerializeField] private AudioClip rotationMiddle;
    [SerializeField] private AudioClip rotationOutro;

    [Header("Buttons")] [SerializeField] private AudioSource button;

    private Coroutine _transitionCoroutine;

    public float OutroLength => rotationOutro != null ? rotationOutro.length : 0f;

    protected override void Awake()
    {
        if (Instance != null && Instance != this)
        {
            AudioManager persistent = Instance;

            // Only overwrite if the new scene actually has these assigned
            if (doorLeft != null)      persistent.doorLeft       = doorLeft;
            if (doorRight != null)     persistent.doorRight      = doorRight;
            if (keyPressSource != null) persistent.keyPressSource = keyPressSource;
            if (keyPressClip != null)  persistent.keyPressClip   = keyPressClip;
            if (rotation != null)      persistent.rotation       = rotation;
            if (rotationIntro != null) persistent.rotationIntro  = rotationIntro;
            if (rotationMiddle != null) persistent.rotationMiddle = rotationMiddle;
            if (rotationOutro != null) persistent.rotationOutro  = rotationOutro;
            if (button != null)        persistent.button         = button;

            // Don't destroy this GameObject — its AudioSources need to stay alive
            // Just destroy this component so there's only one AudioManager
            Destroy(this);
            return;
        }

        base.Awake(); // registers as the singleton + DontDestroyOnLoad
    }

    public void PlayDoorLeft()
    {
        doorLeft.Play();
    }

    public void PlayDoorRight()
    {
        doorRight.Play();
    }

    public void PlayKeyPress()
    {
        keyPressSource.PlayOneShot(keyPressClip);
    }

    public void PlayButton()
    {
        button.Play();
    }

    public void PlayRotationStart()
    {
        // Stop any pending intro->middle transition
        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = null;
        }

        rotation.loop = false;
        rotation.clip = rotationIntro;
        rotation.Play();

        _transitionCoroutine = StartCoroutine(TransitionToMiddle());
    }

    public void PlayRotationEnd()
    {
        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = null;
        }

        if (rotation.clip == rotationIntro && rotation.isPlaying)
            return;

        rotation.loop = false;
        rotation.clip = rotationOutro;
        rotation.Play();
    }

    private IEnumerator TransitionToMiddle()
    {
        yield return new WaitForSeconds(rotationIntro.length - TransitionOffset);

        rotation.clip = rotationMiddle;
        rotation.loop = true;
        rotation.Play();

        _transitionCoroutine = null;
    }
}