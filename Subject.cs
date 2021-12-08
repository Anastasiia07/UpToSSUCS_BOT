namespace TelegramBOT_SSU {
  class Subject {
    private string id;
    private string year;
    private string name;
    private string hours;
    private string course;

    public string Id { get => id; set => id = value; }
    public string Year { get => year; set => year = value; }
    public string Name { get => name; set => name = value; }
    public string Hours { get => hours; set => hours = value; }
    public string Course { get => course; set => course = value; }

    public Subject(string id, string year, string name, string hours, string course) {
      this.Id = id;
      this.Year = year;
      this.Name = name;
      this.Hours = hours;
      this.Course = course;
    }
  }
}
