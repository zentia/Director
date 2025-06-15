using System;

namespace TimelineRuntime
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TimelineItemAttribute : Attribute
    {
        private string subCategory; // Sub category for item
        private string label; // Name of the item
        private TimelineItemGenre[] genres; // Genres that the item belongs to.

        private Type requiredObjectType; 

        public TimelineItemAttribute(string category, string label, params TimelineItemGenre[] genres)
        {
            subCategory = category;
            this.label = label;
            this.genres = genres;
        }

        public TimelineItemAttribute(string category, string label, Type pairedObject, params TimelineItemGenre[] genres)
        {
            subCategory = category;
            this.label = label;
            requiredObjectType = pairedObject;
            this.genres = genres;
        }

        /// <summary>
        /// The category this timeline item belongs in.
        /// </summary>
        public string Category
        {
            get
            {
                return subCategory;
            }
        }

        /// <summary>
        /// The name of this timeline item.
        /// </summary>
        public string Label
        {
            get
            {
                return label;
            }
        }

        /// <summary>
        /// The genres that this timeline item belongs to.
        /// </summary>
        public TimelineItemGenre[] Genres
        {
            get
            {
                return genres;
            }
        }

        /// <summary>
        /// Get the type of the required object that this timeline item should be paired with.
        /// Null when there is no required object.
        /// Example: AudioClip type for PlayAudio.
        /// </summary>
        public Type RequiredObjectType
        {
            get
            {
                return requiredObjectType;
            }
        }
    }
}