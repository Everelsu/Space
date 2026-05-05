using System;

namespace Space
{
    /// <summary>
    /// Глобальное состояние активного таймера задачи.
    /// Живёт на протяжении всей сессии (статический).
    /// </summary>
    public static class TimerService
    {
        public static int     ActiveTaskId    { get; private set; } = -1;
        public static string  ActiveTaskTitle { get; private set; }
        public static DateTime? StartTime     { get; private set; }

        public static bool IsRunning => ActiveTaskId > 0 && StartTime.HasValue;

        public static TimeSpan Elapsed =>
            IsRunning ? DateTime.Now - StartTime.Value : TimeSpan.Zero;

        public static string ElapsedText
        {
            get
            {
                var e = Elapsed;
                return $"{(int)e.TotalHours:D2}ч {(int)e.Minutes:D2}м {e.Seconds:D2}с";
            }
        }

        public static void Start(int taskId, string title)
        {
            ActiveTaskId    = taskId;
            ActiveTaskTitle = title;
            StartTime       = DateTime.Now;
        }

        /// <summary>Останавливает таймер и возвращает округлённые часы.</summary>
        public static decimal Stop()
        {
            if (!IsRunning) return 0;
            var hours = (decimal)Elapsed.TotalHours;
            hours = Math.Round(Math.Max(hours, 0.01m), 2);   // минимум ~1 минута
            ActiveTaskId    = -1;
            ActiveTaskTitle = null;
            StartTime       = null;
            return hours;
        }
    }
}
