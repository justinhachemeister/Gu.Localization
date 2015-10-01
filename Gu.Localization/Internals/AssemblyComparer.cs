﻿namespace Gu.Localization
{
    using System.Collections.Generic;
    using System.Reflection;

    internal class AssemblyComparer : IEqualityComparer<Assembly>
    {
        public static readonly AssemblyComparer Default = new AssemblyComparer();

        public bool Equals(Assembly x, Assembly y)
        {
            return x.FullName == y.FullName;
        }

        public int GetHashCode(Assembly obj)
        {
            return obj.FullName.GetHashCode();
        }
    }
}
