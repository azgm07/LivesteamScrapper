namespace LivesteamScrapper.Models
{
    public class EnumsModel
    {
        public enum TimerLog
        {
            Start,
            Stop,
            Lap
        }

        public enum BrowserLog
        {
            Ready,
            NotReady,
            Reloading
        }

        public enum ScrapperLog
        {
            Started,
            Running,
            FailedToStart,
            Failed,
            Stopped
        }

        public enum ScrapperMode
        {
            Off,
            Viewers,
            Chat,
            All
        }
    }
}