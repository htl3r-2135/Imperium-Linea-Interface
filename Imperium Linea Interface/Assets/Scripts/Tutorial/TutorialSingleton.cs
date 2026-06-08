using Abstract;

namespace Tutorial
{
    public class TutorialSingleton : Singleton<TutorialSingleton>
    {
        private bool _isTutorial = false;
        
        private bool _blockSpawn = false;
        
        private bool _blockDoorsClose = false;
        
        private bool _blockDoorsOpen = false;
        
        private bool _blockDoorsLock = false;
        
        private bool _blockRotate = false;
        
        private bool _lookBlock = false;
        
        private bool _moveBlock = false;

        public bool IsTutorial()
        {
            return _isTutorial;
        }
        
        public bool GetSpawnBlock()
        {
            return _blockSpawn;
        }

        public bool GetDoorsCloseBlock()
        {
            return _blockDoorsClose;
        }

        public bool GetDoorsOpenBlock()
        {
            return _blockDoorsOpen;
        }

        public bool GetDoorsLock()
        {
            return _blockDoorsLock;
        }

        public bool GetRotateBlock()
        {
            return _blockRotate;
        }

        public bool GetLookBlock()
        {
            return _lookBlock;
        }

        public bool GetMoveBlock()
        {
            return _moveBlock;
        }

        public void SetIsTutorial(bool value)
        {
            _isTutorial = value;
        }
        
        public void SetSpawnBlock(bool value)
        {
            _blockSpawn = value;
        }

        public void SetDoorsCloseBlock(bool value)
        {
            _blockDoorsClose = value;
        }

        public void SetDoorsOpenBlock(bool value)
        {
            _blockDoorsOpen = value;
        }

        public void SetDoorsLock(bool value)
        {
            _blockDoorsLock = value;
        }

        public void SetRotateBlock(bool value)
        {
            _blockRotate = value;
        }

        public void SetLookBlock(bool value)
        {
            _lookBlock = value;
        }

        public void SetMoveBlock(bool value)
        {
            _moveBlock = value;
        }
    }
}

