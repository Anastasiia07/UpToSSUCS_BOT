namespace TelegramBOT_SSU {
  class UserIDIndexHelper {
    private string chat_id;
    private int index = 0;

    public int Index { get => index; set => index = value; }
    public string Chat_id { get => chat_id; set => chat_id = value; }

    public UserIDIndexHelper(string chat_id) {
      this.chat_id = chat_id;
    }
  }
}
