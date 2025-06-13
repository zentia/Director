namespace TimelineEditorInternal
{
    static internal class TimelineWindowUtility
    {
        // We automatically group Vector4, Vector3 and Color
        static public int GetComponentIndex(string name)
        {
            if (name == null || name.Length < 3 || name[name.Length - 2] != '.')
                return -1;
            char lastCharacter = name[name.Length - 1];
            switch (lastCharacter)
            {
                case 'r':
                    return 0;
                case 'g':
                    return 1;
                case 'b':
                    return 2;
                case 'a':
                    return 3;
                case 'x':
                    return 0;
                case 'y':
                    return 1;
                case 'z':
                    return 2;
                case 'w':
                    return 3;
                default:
                    return -1;
            }
        }
        // If Vector4, Vector3 or Color, return group name instead of full name
        public static string GetPropertyGroupName(string propertyName)
        {
            if (GetComponentIndex(propertyName) != -1)
                return propertyName.Substring(0, propertyName.Length - 2);

            return propertyName;
        }

    }
}

