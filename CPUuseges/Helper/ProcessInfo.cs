namespace CPUuseges.Helper
{
    public struct ProcessInfo
    {
        public string Name { get; set; }
        public int Pid { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is ProcessInfo other)
            {
                return Pid.Equals(other.Pid);
            }
            else
                return false;
        }

        public override int GetHashCode()
        {
            return Pid.GetHashCode();
        }

        public static bool operator ==(ProcessInfo left, ProcessInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ProcessInfo left, ProcessInfo right)
        {
            return !(left == right);
        }
    }
}
