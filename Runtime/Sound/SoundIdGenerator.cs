namespace AudioTools.Sound
{
    public class SoundIdGenerator : ISoundIdGenerator
    {
        private int currentId = 0;
        
        public int GetNextId()
        {
            return ++currentId;
        }
    }
}