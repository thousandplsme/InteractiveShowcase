using System;

namespace InteractiveShowcase.Prototype1
{
    public enum ExperienceState { Offline, Ready, Qualifying, Greeting, AwaitExit }

    // All timestamps are monotonic seconds, supplied by the caller (or a test clock).
    public sealed class ExperienceStateMachine
    {
        private readonly double hold, greeting, exit;
        private double faceSince = double.NaN, absentSince = double.NaN, greetingSince;
        public ExperienceState State { get; private set; } = ExperienceState.Offline;
        public int StartCount { get; private set; }
        public bool IsGreeting => State == ExperienceState.Greeting;

        public ExperienceStateMachine(double hold = 1, double greeting = 2, double exit = 1)
        {
            foreach (double value in new[] { hold, greeting, exit })
                if (double.IsNaN(value) || double.IsInfinity(value) || value < .1 || value > 10)
                    throw new ArgumentOutOfRangeException(nameof(hold), "Durations must be finite and 0.1..10 seconds.");
            this.hold = hold; this.greeting = greeting; this.exit = exit;
        }

        public void Reset()
        {
            State = ExperienceState.Offline;
            faceSince = absentSince = double.NaN;
            greetingSince = 0;
            StartCount = 0;
        }

        public bool Tick(double now, bool connected, bool face)
        {
            if (double.IsNaN(now) || double.IsInfinity(now)) throw new ArgumentOutOfRangeException(nameof(now));
            // A disconnect resets timing, but preserves the session's diagnostic start count.
            if (!connected)
            {
                State = ExperienceState.Offline;
                faceSince = absentSince = double.NaN;
                greetingSince = 0;
                return false;
            }
            if (State == ExperienceState.Offline) State = ExperienceState.Ready;
            if (State == ExperienceState.Ready || State == ExperienceState.Qualifying)
            {
                if (!face) { State = ExperienceState.Ready; faceSince = double.NaN; return false; }
                if (State == ExperienceState.Ready) { State = ExperienceState.Qualifying; faceSince = now; }
                if (now - faceSince < hold) return false;
                State = ExperienceState.Greeting;
                greetingSince = now;
                absentSince = double.NaN;
                StartCount++;
                return true;
            }
            if (face) absentSince = double.NaN;
            else if (double.IsNaN(absentSince)) absentSince = now;
            if (State == ExperienceState.Greeting && now - greetingSince < greeting) return false;
            State = !face && now - absentSince >= exit ? ExperienceState.Ready : ExperienceState.AwaitExit;
            if (State == ExperienceState.Ready) { faceSince = absentSince = double.NaN; }
            return false;
        }
    }
}
