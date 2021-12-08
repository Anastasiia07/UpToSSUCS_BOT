namespace TelegramBOT_SSU {
  class Question {
    private string question_id;
    private string subject_id;
    private string year;
    private string question_name;
    private string option_1;
    private string option_2;
    private string option_3;
    private string option_4;
    private string correct_answer;

    public string Question_id { get => question_id; set => question_id = value; }
    public string Subject_id { get => subject_id; set => subject_id = value; }
    public string Year { get => year; set => year = value; }
    public string Question_name { get => question_name; set => question_name = value; }
    public string Option_1 { get => option_1; set => option_1 = value; }
    public string Option_2 { get => option_2; set => option_2 = value; }
    public string Option_3 { get => option_3; set => option_3 = value; }
    public string Option_4 { get => option_4; set => option_4 = value; }
    public string Correct_answer { get => correct_answer; set => correct_answer = value; }

    public Question(string question_id, string subject_id, string year, string question_name, 
      string option_1, string option_2, string option_3, string option_4, string correct_answer) {
      this.Question_id = question_id;
      this.Subject_id = subject_id;
      this.Year = year;
      this.Question_name = question_name;
      this.Option_1 = option_1;
      this.Option_2 = option_2;
      this.Option_3 = option_3;
      this.Option_4 = option_4;
      this.Correct_answer = correct_answer;
    }
  }
}
