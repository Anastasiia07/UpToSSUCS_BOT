namespace TelegramBOT_SSU {
  class Points {
    private string point_id;
    private string chat_id;
    private string subject_id;
    private string result;
    private string time;

    public string Point_id { get => point_id; set => point_id = value; }
    public string Chat_id { get => chat_id; set => chat_id = value; }
    public string Subject_id { get => subject_id; set => subject_id = value; }
    public string Result { get => result; set => result = value; }
    public string Time { get => time; set => time = value; }

    public Points(string point_id, string chat_id, string subject_id, string result, string time) {
      this.point_id = point_id;
      this.chat_id = chat_id;
      this.subject_id = subject_id;
      this.result = result;
      this.time = time;
    }
  }
}
