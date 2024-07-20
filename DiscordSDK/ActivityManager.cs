using System;

namespace Discord
{
    public partial class ActivityManager
    {
        public void RegisterCommand()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            RegisterCommand(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
    }
}
