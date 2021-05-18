using UnityEngine;

namespace VamTimeline
{
    public class StyleBase
    {
        // Global
        public Font Font { get; }
        public virtual Color FontColor { get; } = new Color(0, 0, 0);
        public virtual Color BackgroundColor { get; } = new Color(0.721f, 0.682f, 0.741f);

        public StyleBase()
        {
            Font = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
        }
    }
}
