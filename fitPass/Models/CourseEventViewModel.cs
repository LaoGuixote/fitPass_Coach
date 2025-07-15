namespace fitPass.Models
{
    public class CourseEventViewModel
    {
        public string CourseTitle { get; set; }
        public DateOnly ClassDate { get; set; }
        public int TimeSlot { get; set; }
        public string CoachName { get; set; }

        public int ClassTimeDaily { get; set; }    // 上課時段
        public string Title { get; set; }              // 課程標題（for Razor用）
        public int CourseId { get; set; }

        // 可以再加其他你想顯示的欄位
    }
}
