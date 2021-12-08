using System;
using System.Collections.Generic;
using System.IO;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.ReplyMarkups;
using System.Data.SQLite;

namespace TelegramBOT_SSU {
  class Program {
    private static readonly string token = "1902130562:AAGFSx-ok7Jc6eaKNhNExV9XmVM2XAsqau4";
    private static TelegramBotClient client;
    private static string msg;
    private static MessageEventArgs msgEvent;
    private static bool checkingForRegistrationInput = false;
    private static bool checkingForChangingSettingsInput = false;
    private static bool checkingForAdminInput = false;
    private static bool checkingForSubjectInput = false;
    private static bool isMakeUserAdmin = false;
    private static string tempCourse;
    private static string tempCollege;
    private static string tempSpecialty;
    private static bool waitingForNewOwnCollegeName = false;
    private static bool waitingForNewOwnSpecialtyName = false;
    private static SQLiteConnection DB;
    private static readonly string pathDB = "Data Source=" + Environment.CurrentDirectory + "/DB.db;";
    private static List<Subject> allSubjects;
    private static List<UserIDIndexHelper> indexList = new ();
    private static bool waitingForQuestion = false;
    private static Question currentQuestion;
    private static int currentIndexForQuestions = 0;
    private static int currentPointsForQuestion = 0;
    private static List<Question> questions;

    static void Main() {
      client = new TelegramBotClient(token);
      client.StartReceiving();
      client.OnMessage += OnMessageHandler;
      Console.ReadLine();
      client.StopReceiving();
    }

    private static bool CheckForUserInMemory(string chat_id) {
      bool alreadyExist = false;
      for (int i = 0; i < indexList.Count; i++) {
        if (indexList[i].Chat_id.Equals(chat_id)) {
          alreadyExist = true;
        }
      }
      return alreadyExist;
    }

    private static void AddUserToMemory(string chat_id) {
      if (!CheckForUserInMemory(chat_id)) {
        UserIDIndexHelper userIDIndexHelper = new(chat_id);
        indexList.Add(userIDIndexHelper);
      }
    }

    private static async void OnMessageHandler(object sender, MessageEventArgs e) {
      msg = e.Message.Text;
      msgEvent = e;
      if (msg.Contains(TextConstants.startMessage)) { //начало работы
        AddUserToMemory(msgEvent.Message.Chat.Id.ToString());
        if (IsRegisteredAlready(msgEvent.Message.Chat.Id.ToString())) {
          SendSimpleMessageWithRedirection("З поверненям, " + msgEvent.Message.Chat.FirstName.ToString() + "!");
        } else {
          SendSimpleMessageWithoutRedirection(TextConstants.messageHelloFirst + "\n\n" + TextConstants.messageHelloSecond);
          checkingForRegistrationInput = true;
          GoToChooseCollege();
        }
      } else if (msg.Contains(TextConstants.messageAgreeForUpdating)) {
        GoToChooseSubjects(msgEvent.Message.Chat.Id.ToString());
      } else if (msg.Contains(TextConstants.back)) { //выйти в главное меню
        SendMessageAboutChoosingAction();
      } else if (msg.Contains(" годин або більше")) {
        for (int i = 0; i < indexList.Count; i++) {
          if (indexList[i].Chat_id.Equals(msgEvent.Message.Chat.Id.ToString())) {
            RegisterSubjectInPoints(indexList[i].Chat_id, allSubjects[indexList[i].Index].Id, 3);
            indexList[i].Index++;
            break;
          }
        }
        AskingAboutSubjects(msgEvent.Message.Chat.Id.ToString());
      } else if (waitingForQuestion) {
        if (msg.Equals(currentQuestion.Correct_answer)) {
          currentPointsForQuestion++;
        }
        
        currentIndexForQuestions++;
        if (currentIndexForQuestions > 2) {
          waitingForQuestion = false;
          EnterNewResultForCurrentSubject(currentPointsForQuestion.ToString(), currentQuestion.Subject_id);
          int tempPointsValue = currentPointsForQuestion;
          currentPointsForQuestion = 0;
          currentIndexForQuestions = 0;
          SendSimpleMessageWithRedirection("Ви пройшли тест з " + GetSubjectNameById(currentQuestion.Subject_id) + " і набрали таку кількість правильних відповідей: " + tempPointsValue);
        } else {
          currentQuestion = questions[currentIndexForQuestions];
          await client.SendTextMessageAsync(msgEvent.Message.Chat.Id, questions[currentIndexForQuestions].Question_name, replyMarkup: GetTestForm(questions[currentIndexForQuestions]));
        }
      } else if (msg.Contains("Я вивчав цей предмет менше ніж ") || msg.Contains("Я не вивчав цей предмет взагалі")) {
        for (int i = 0; i < indexList.Count; i++) {
          if (indexList[i].Chat_id.Equals(msgEvent.Message.Chat.Id.ToString())) {
            RegisterSubjectInPoints(indexList[i].Chat_id, allSubjects[indexList[i].Index].Id, 0);
            indexList[i].Index++;
            break;
          }
        }
        AskingAboutSubjects(msgEvent.Message.Chat.Id.ToString());
      } else if (msg.Contains(TextConstants.newCollegeInput) && (checkingForRegistrationInput || checkingForChangingSettingsInput)) { //ввод собственного колледжа
        SendSimpleMessageWithoutRedirection(TextConstants.messageAskForCollegeName);
        waitingForNewOwnCollegeName = true;
      } else if (waitingForNewOwnCollegeName) {
        if (checkingForRegistrationInput) {
          tempCollege = msg;
          waitingForNewOwnCollegeName = false;
          SpecialtyRegistration();
        } else {
          checkingForChangingSettingsInput = false;
          waitingForNewOwnCollegeName = false;
          SetCollege(msgEvent.Message.Chat.Id.ToString(), msg.ToString());
          SendMessageAboutChoosingAction();
        }
      } else if (msg.Contains(TextConstants.specialtyUnknown) && (checkingForRegistrationInput || checkingForChangingSettingsInput)) { //ввод собственной специальности
        SendSimpleMessageWithoutRedirection(TextConstants.specialtyEnterName);
        waitingForNewOwnSpecialtyName = true;
      } else if (waitingForNewOwnSpecialtyName) {
        if (checkingForRegistrationInput) {
          tempSpecialty = msg;
          waitingForNewOwnSpecialtyName = false;
          CourseRegistration();
        } else {
          checkingForChangingSettingsInput = false;
          waitingForNewOwnCollegeName = false;
          SetCollege(msgEvent.Message.Chat.Id.ToString(), msg.ToString());
          SendMessageAboutChoosingAction();
        }
      } else if ((msg.Contains(TextConstants.college1) || msg.Contains(TextConstants.college2) || msg.Contains(TextConstants.college3))
            && (checkingForRegistrationInput || checkingForChangingSettingsInput)) { //регистрация колледжа
        if (checkingForRegistrationInput) {
          tempCollege = msg;
          SendSimpleMessageWithoutRedirection(TextConstants.specialtyPreEnterName);
          SpecialtyRegistration();
        } else {
          checkingForChangingSettingsInput = false;
          SetCollege(msgEvent.Message.Chat.Id.ToString(), msg.ToString());
          SendSimpleMessageWithoutRedirection(TextConstants.messageCollegeWasChanged);
          SendMessageAboutChoosingAction();
        }
      } else if ((msg.Contains(TextConstants.specialty122) || msg.Contains(TextConstants.specialty123) || msg.Contains(TextConstants.specialty151))
            && (checkingForRegistrationInput || checkingForChangingSettingsInput)) { //регистрация специальности
        if (checkingForRegistrationInput) {
          tempSpecialty = msg;
          CourseRegistration();
        } else {
          checkingForChangingSettingsInput = false;
          SetSpecialty(msgEvent.Message.Chat.Id.ToString(), msg.ToString());
          SendSimpleMessageWithRedirection(TextConstants.specialtyWasChanged);
        }
      } else if ((msg.Contains(TextConstants.secondCourse) || msg.Contains(TextConstants.thirdCourse))
            && checkingForRegistrationInput) { //регистрация курса
        if (checkingForRegistrationInput) {
          tempCourse = msg.Substring(0, 1);
          checkingForRegistrationInput = false;
          CreateUser(msgEvent.Message.Chat.Id.ToString(), msgEvent.Message.Chat.FirstName.ToString(),
              tempCourse.ToString(), tempCollege.ToString(), tempSpecialty.ToString());
          ChoosingSubjects();
        }
      } else if (msg.Contains(TextConstants.adminMessage)) { //админка
        if (GetAdmin(msgEvent.Message.Chat.Id.ToString())) {
          checkingForAdminInput = true;
          await client.SendTextMessageAsync(msgEvent.Message.Chat.Id, TextConstants.chooseAction, replyMarkup: GetAdminPanels());
        } else {
          SendSimpleMessageWithRedirection(TextConstants.notAdminMessage);
        }
      } else if (msg.Contains(TextConstants.changeExcelsAdminMessage) && checkingForAdminInput) {
        await client.SendTextMessageAsync(msgEvent.Message.Chat.Id, TextConstants.chooseAction, replyMarkup: GetDBUpdateFromExcelsButtons());
      } else if (msg.Contains(TextConstants.newPlanExcelFile) && checkingForAdminInput) {
        RemoveAllPoints();
        DoSubjectsExcelToDB();
        DoQuestionsExcelToDB();
        DoDisciplinesProgramExcelToDB();
        checkingForAdminInput = false;
        SendSimpleMessageWithoutRedirection(TextConstants.updatedInfoPlan);
        NotificateAboutNewPlan();
      } else if (msg.Contains(TextConstants.newQuestiongsExcelFile) && checkingForAdminInput) {
        DoQuestionsExcelToDB();
        checkingForAdminInput = false;
        SendSimpleMessageWithRedirection(TextConstants.updatedInfoQuestions);
      } else if (msg.Contains(TextConstants.newDisciplineExcelFile) && checkingForAdminInput) {
        DoDisciplinesProgramExcelToDB();
        checkingForAdminInput = false;
        SendSimpleMessageWithRedirection(TextConstants.updatedInfoDisciplineProgram);
      } else if (msg.Contains(TextConstants.addAdmin) && checkingForAdminInput) { //дать или забрать админку по айдишнику
        isMakeUserAdmin = true;
        SendSimpleMessageWithoutRedirection(TextConstants.enterUserIDForAdmin);
      } else if (msg.Contains(TextConstants.removeAdmin) && checkingForAdminInput) { //дать или забрать админку по айдишнику
        isMakeUserAdmin = false;
        SendSimpleMessageWithoutRedirection(TextConstants.enterUserIDForAdmin);
      } else if (msg.Contains(TextConstants.settings)) { //настройки
        checkingForChangingSettingsInput = true;
        await client.SendTextMessageAsync(msgEvent.Message.Chat.Id, TextConstants.chooseSetting, replyMarkup: GetSettings());
      } else if (msg.Contains(TextConstants.changeSpecialty) && checkingForChangingSettingsInput) { //выбрать специальность
        GoToChooseSpecialty();
      } else if (msg.Contains(TextConstants.changeCollege) && checkingForChangingSettingsInput) { //выбрать колледж
        GoToChooseCollege();
      } else if (msg.Contains(TextConstants.chooseCourse) && checkingForChangingSettingsInput) { //выбрать курс
        checkingForChangingSettingsInput = false;
        string currentCourse = GetCourse(msgEvent.Message.Chat.Id.ToString());
        if (currentCourse.Equals("3")) {
          RemoveThirdCourseFromPoints(msgEvent.Message.Chat.Id.ToString());
          SetCourse(msgEvent.Message.Chat.Id.ToString(), "2");
          SendSimpleMessageWithRedirection(TextConstants.messageCourseWasChanged);
        } else {
          for (int i = 0; i < indexList.Count; i++) {
            if (indexList[i].Chat_id.Equals(msgEvent.Message.Chat.Id.ToString())) {
              indexList[i].Index = 0;
              break;
            }
          }
          SetCourse(msgEvent.Message.Chat.Id.ToString(), "3");
          GoToChooseThirdCourseSubjects(msgEvent.Message.Chat.Id.ToString());
        }
      } else if (msg.Contains(TextConstants.messageStartTesting)) {
        questions = GetQuestionsForSubjectBySubjectID(GetSubjectIDByName(msg[TextConstants.messageStartTesting.Length..]));
        waitingForQuestion = true;
        currentQuestion = questions[currentIndexForQuestions];
        await client.SendTextMessageAsync(msgEvent.Message.Chat.Id, questions[currentIndexForQuestions].Question_name, replyMarkup: GetTestForm(questions[currentIndexForQuestions]));
      } else if (msg.Contains(TextConstants.goToStyding)) { //перейти к списку предметов для обучения
        checkingForSubjectInput = true;
        await client.SendTextMessageAsync(msgEvent.Message.Chat.Id, TextConstants.chooseSubject,
            replyMarkup: GetKeyboard(GetNotPassedSubjects(msgEvent.Message.Chat.Id.ToString())));
      } else if (msg.Contains(TextConstants.results)) { //вывод результатов
        string userScore = "Ім'я користувача: " + msgEvent.Message.Chat.FirstName.ToString();
        userScore += "\nId користувача:  " + msgEvent.Message.Chat.Id.ToString();
        userScore += "\nКоледж користувача: " + GetCollege(msgEvent.Message.Chat.Id.ToString());
        userScore += "\nСпеціальність користувача: " + GetSpecialty(msgEvent.Message.Chat.Id.ToString());
        userScore += "\nОбраний користувачем курс: " + GetCourse(msgEvent.Message.Chat.Id.ToString());
        userScore += "\n";
        userScore += "\nЗараховані предмети користувача:";
        List<Points> userPoints = GetAllPoints(msgEvent.Message.Chat.Id.ToString());
        int tempCounter = 1;
        for (int i = 0; i < userPoints.Count; i++) {
          if (userPoints[i].Result.Equals("3")) {
            userScore += "\n" + tempCounter++ + ". " + GetSubjectNameById(userPoints[i].Subject_id);
          }
        }
        userScore += "\n";
        userScore += "\nНезараховані предмети користувача:";
        tempCounter = 1;
        for (int i = 0; i < userPoints.Count; i++) {
          if (!userPoints[i].Result.Equals("3")) {
            userScore += "\n" + tempCounter++ + ". " + GetSubjectNameById(userPoints[i].Subject_id) + " (з поточною оцінкою — "
                + userPoints[i].Result + "/3)";
          }
        }
        SendSimpleMessageWithRedirection(userScore);
      } else if (checkingForAdminInput) {
        if (int.TryParse(msg, out _)) {
          if (isMakeUserAdmin) {
            MakeUserAdmin(msg);
          } else {
            MakeUserNotAdmin(msg);
          }
          checkingForAdminInput = false;
          SendSimpleMessageWithRedirection(TextConstants.changeAdminMessage);
        } else {
          SendSimpleMessageWithRedirection(TextConstants.errorNotFoundUser);
        }
      } else if (checkingForSubjectInput) {
        checkingForSubjectInput = false;
        List<string> disciplineProgramList = GetDisciplineProgram(GetSubjectIDByName(msg));
        SendSimpleMessageWithoutRedirection(disciplineProgramList[0] + "\n\n" + disciplineProgramList[1]);
        await client.SendTextMessageAsync(msgEvent.Message.Chat.Id, TextConstants.chooseAction, replyMarkup: GetSubjectButtons(msg));
      } else { //если данная команда не обрабатываемая
        SendSimpleMessageWithRedirection(TextConstants.errorMessage);
      }
    }

    private static async void NotificateAboutNewPlan() {
      List<string> allUsersID = GetAllUsersID();
      for (int i = 0; i < allUsersID.Count; i++) {
        await client.SendTextMessageAsync(allUsersID[i], TextConstants.messageChangedPlan, replyMarkup: GetAgreeForUpdatingSubjects());        
      }
    }

    private static IReplyMarkup GetAgreeForUpdatingSubjects() {
      return new ReplyKeyboardMarkup {
        Keyboard = new List<List<KeyboardButton>>
        {
          new List<KeyboardButton>
          {
            new KeyboardButton { Text = TextConstants.messageAgreeForUpdating }
          }
        }
      };
    }

    private static void SpecialtyRegistration() {
      GoToChooseSpecialty();
    }

    private static void CourseRegistration() {
      SendSimpleMessageWithoutRedirection(TextConstants.messageChooseCourseFirst + "\n\n" +
        TextConstants.messageChooseCourseSecond + "\n\n" +
        TextConstants.messageChooseCourseThird + "\n\n" +
        TextConstants.messageChooseCourseFourth);
      GoToChooseCourse();
    }

    private static void ChoosingSubjects() {
      SendSimpleMessageWithoutRedirection(TextConstants.messageChooseSubjects1 + "\n\n" + 
        TextConstants.messageChooseSubjects2 + "\n\n" + 
        TextConstants.messageChooseSubjects3 + "\n\n" +
        TextConstants.messageChooseSubjects4);
      GoToChooseSubjects(msgEvent.Message.Chat.Id.ToString());
    }

    private static async void SendSimpleMessageWithoutRedirection(String message) {
      await client.SendTextMessageAsync(msgEvent.Message.Chat.Id, message);
    }

    private static async void SendSimpleMessageWithRedirection(String message) {
      await client.SendTextMessageAsync(msgEvent.Message.Chat.Id, message);
      SendMessageAboutChoosingAction();
    }

    public static ReplyKeyboardMarkup GetKeyboard(List<string> keys) {
      var rkm = new ReplyKeyboardMarkup();
      var rows = new List<KeyboardButton[]>();
      var cols = new List<KeyboardButton>();
      for (int i = 1; i <= keys.Count; i++) {
        cols.Add(new KeyboardButton(keys[i - 1]));
        if (i % 2 != 0 && i != keys.Count)
          continue;
        rows.Add(cols.ToArray());
        cols = new List<KeyboardButton>();
      }
      cols = new List<KeyboardButton> {
        new KeyboardButton(TextConstants.back)
      };
      rows.Add(cols.ToArray());
      rkm.Keyboard = rows.ToArray();
      return rkm;
    }

    private static IReplyMarkup GetSubjectButtons(string subject_name) {
      return new ReplyKeyboardMarkup {
        Keyboard = new List<List<KeyboardButton>>
        {
          new List<KeyboardButton>
          {
            new KeyboardButton { Text = TextConstants.messageStartTesting + subject_name}
          },
          new List<KeyboardButton>
          {
            new KeyboardButton { Text = TextConstants.back }
          },
        }
      };
    }
    private static IReplyMarkup GetDBUpdateFromExcelsButtons() {
      return new ReplyKeyboardMarkup {
        Keyboard = new List<List<KeyboardButton>>
        {
          new List<KeyboardButton>
          {
            new KeyboardButton { Text = TextConstants.newPlanExcelFile }
          },
          new List<KeyboardButton>
          {
            new KeyboardButton { Text = TextConstants.newDisciplineExcelFile }
          },
          new List<KeyboardButton>
          {
            new KeyboardButton { Text = TextConstants.newQuestiongsExcelFile }
          },
          new List<KeyboardButton>
          {
            new KeyboardButton { Text = TextConstants.back }
          },
        }
      };
    }

    private static async void SendMessageAboutChoosingAction() {
      await client.SendTextMessageAsync(msgEvent.Message.Chat.Id, TextConstants.chooseAction, replyMarkup: GetMainMenuButtons());
    }

    private static IReplyMarkup GetMainMenuButtons() {
      return new ReplyKeyboardMarkup {
        Keyboard = new List<List<KeyboardButton>>
        {
          new List<KeyboardButton>
          {
            new KeyboardButton { Text = TextConstants.goToStyding }
          },
          new List<KeyboardButton>
          {
            new KeyboardButton { Text = TextConstants.results },
            new KeyboardButton { Text = TextConstants.settings }
          }
        }
      };
    }

    private static async void GoToChooseSpecialty() {
      await client.SendTextMessageAsync(msgEvent.Message.Chat.Id, TextConstants.specialtyEnterName, replyMarkup: GetSpecialties());
    }

    private static IReplyMarkup GetSpecialties() {
      return new ReplyKeyboardMarkup {
        Keyboard = new List<List<KeyboardButton>>
        {
          new List<KeyboardButton>
          {
            new KeyboardButton { Text = TextConstants.specialty122 }
          },
          new List<KeyboardButton>
          {
            new KeyboardButton { Text = TextConstants.specialty123 }
          },
          new List<KeyboardButton>
          {
            new KeyboardButton { Text = TextConstants.specialty151 }
          },
          new List<KeyboardButton>
          {
            new KeyboardButton { Text = TextConstants.specialtyUnknown }
          }
        }
      };
    }

    private static async void GoToChooseCollege() {
      await client.SendTextMessageAsync(msgEvent.Message.Chat.Id, TextConstants.messageChooseCollege, replyMarkup: GetColleges());
    }

    private static IReplyMarkup GetColleges() {
      return new ReplyKeyboardMarkup {
        Keyboard = new List<List<KeyboardButton>>
        {
          new List<KeyboardButton>
          {
            new KeyboardButton { Text = TextConstants.college1 }
          },
          new List<KeyboardButton>
          {
            new KeyboardButton { Text = TextConstants.college2 }
          },
          new List<KeyboardButton>
          {
            new KeyboardButton { Text = TextConstants.college3 }
          },
          new List<KeyboardButton>
          {
            new KeyboardButton { Text = TextConstants.newCollegeInput }
          }
        }
      };
    }

    private static IReplyMarkup GetTestForm(Question question) {
      return new ReplyKeyboardMarkup {
        Keyboard = new List<List<KeyboardButton>>
        {
          new List<KeyboardButton>
          {
            new KeyboardButton { Text = question.Option_1 }
          },
          new List<KeyboardButton>
          {
            new KeyboardButton { Text = question.Option_2 }
          },
          new List<KeyboardButton>
          {
            new KeyboardButton { Text = question.Option_3 }
          },
          new List<KeyboardButton>
          {
            new KeyboardButton { Text = question.Option_4 }
          }
        }
      };
    }

    private static void GoToChooseSubjects(string chat_id) {
      for (int i = 0; i < indexList.Count; i++) {
        if (indexList[i].Chat_id.Equals(chat_id)) {
          indexList[i].Index = 0;
          break;
        }
      }
      allSubjects = GetAllSubjects();
      if (GetCourse(chat_id).Equals("2")) {
        allSubjects.RemoveAll(a => a.Course.Equals("3"));
      }
      AskingAboutSubjects(chat_id);
    }

    private static void GoToChooseThirdCourseSubjects(string chat_id) {
      allSubjects = GiveThirdCourseSubjectsList();
      AskingAboutSubjects(chat_id);
    }

    private static async void AskingAboutSubjects(string chat_id) {
      for (int i = 0; i < indexList.Count; i++) {
        if (indexList[i].Chat_id.Equals(chat_id)) {
          if (indexList[i].Index < allSubjects.Count) {
            await client.SendTextMessageAsync(chat_id, TextConstants.askAboutSubject + allSubjects[indexList[i].Index].Name,
                replyMarkup: GetSubjects(allSubjects[indexList[i].Index].Hours));
          } else {
            SendMessageAboutChoosingAction();
          }
          break;
        }
      }
    }

    private static IReplyMarkup GetSubjects(string hours) {
      return new ReplyKeyboardMarkup {
        Keyboard = new List<List<KeyboardButton>>
        {
          new List<KeyboardButton>
          {
            new KeyboardButton { Text = $"Я вивчав цей предмет {hours} годин або більше" }
          },
          new List<KeyboardButton>
          {
            new KeyboardButton { Text = $"Я вивчав цей предмет менше ніж {hours} годин" }
          },
          new List<KeyboardButton>
          {
            new KeyboardButton { Text = "Я не вивчав цей предмет взагалі" }
          }
        }
      };
    }

    private static async void GoToChooseCourse() {
      await client.SendTextMessageAsync(msgEvent.Message.Chat.Id, TextConstants.messageChooseCourse, replyMarkup: GetCourses());
    }

    private static IReplyMarkup GetCourses() {
      return new ReplyKeyboardMarkup {
        Keyboard = new List<List<KeyboardButton>>
        {
          new List<KeyboardButton>
          {
            new KeyboardButton { Text = TextConstants.secondCourse }
          },
          new List<KeyboardButton>
          {
            new KeyboardButton { Text = TextConstants.thirdCourse }
          }
        }
      };
    }

    private static IReplyMarkup GetAdminPanels() {
      return new ReplyKeyboardMarkup {
        Keyboard = new List<List<KeyboardButton>>
        {
          new List<KeyboardButton>
          {
            new KeyboardButton { Text = TextConstants.changeExcelsAdminMessage }
          },
          new List<KeyboardButton>
          {
            new KeyboardButton { Text = TextConstants.addAdmin }
          },
          new List<KeyboardButton>
          {
            new KeyboardButton { Text = TextConstants.removeAdmin }
          },
          new List<KeyboardButton> {
            new KeyboardButton { Text = TextConstants.back }
          }
        }
      };
    }

    private static IReplyMarkup GetSettings() {
      return new ReplyKeyboardMarkup {
        Keyboard = new List<List<KeyboardButton>>
        {
          new List<KeyboardButton>
          {
            new KeyboardButton { Text = TextConstants.changeCollege },
            new KeyboardButton { Text = TextConstants.changeSpecialty }
          },

          new List<KeyboardButton>
          {
            new KeyboardButton { Text = TextConstants.chooseCourse }
          },

          new List<KeyboardButton> {
            new KeyboardButton { Text = TextConstants.back }
          }
        }
      };
    }

    private static string CheckValueExcel(string testValueExcel) {
      if (testValueExcel != "" && testValueExcel != null) {
        if (testValueExcel.Equals("1") || testValueExcel.Equals("2") || testValueExcel.Equals("1-2")) {
          return "1";
        } else if (testValueExcel.Equals("3") || testValueExcel.Equals("4") || testValueExcel.Equals("3-4")) {
          return "2";
        } else if (testValueExcel.Equals("5") || testValueExcel.Equals("6") || testValueExcel.Equals("5-6")) {
          return "3";
        }
      }
      return "";
    }

    private static void DoDisciplinesProgramExcelToDB() {
      try {
        DeleteDeprecatedData("DisciplineProgram");
        using ExcelHelper helper = new();
        if (helper.Open(filePath: Path.Combine(Environment.CurrentDirectory, "Програма_дисциплін.xlsx"))) {
          for (int i = 2; i < 100; i++) {
            if (!helper.Get(column: "A", row: i).Equals("") && helper.Get(column: "A", row: i) != null) {
              CreateDisciplineProgram(GetSubjectIDByName(helper.Get(column: "A", row: i)), DateTime.Now.Year, helper.Get(column: "C", row: i),
                  helper.Get(column: "D", row: i));
            } else {
              break;
            }
          }
        }
      } catch (Exception ex) { Console.WriteLine(ex.Message); }
    }

    private static void DoQuestionsExcelToDB() {
      try {
        DeleteDeprecatedData("Questions");
        using ExcelHelper helper = new();
        if (helper.Open(filePath: Path.Combine(Environment.CurrentDirectory, "Питання_та_відповіді.xlsx"))) {
          for (int i = 2; i < 1000; i++) {
            if (!helper.Get(column: "A", row: i).Equals("") && helper.Get(column: "A", row: i) != null) {
              CreateQuestion(GetSubjectIDByName(helper.Get(column: "A", row: i)), DateTime.Now.Year, helper.Get(column: "C", row: i),
                  helper.Get(column: "D", row: i), helper.Get(column: "E", row: i), helper.Get(column: "F", row: i), helper.Get(column: "G", row: i),
                  helper.Get(column: "H", row: i));
            } else {
              break;
            }
          }
        }
      } catch (Exception ex) { Console.WriteLine(ex.Message); }
    }

    private static void DoSubjectsExcelToDB() {
      try {
        DeleteDeprecatedData("Subjects");
        using ExcelHelper helper = new();
        if (helper.Open(filePath: Path.Combine(Environment.CurrentDirectory, "План.xlsx"))) {
          for (int i = 13; i < 100; i++) {
            if (!CheckValueExcel(helper.Get(column: "D", row: i)).Equals("")) {
              CreateSubject(2021, helper.Get(column: "B", row: i), helper.Get(column: "H", row: i),
                  CheckValueExcel(helper.Get(column: "D", row: i)));
            } else if (!CheckValueExcel(helper.Get(column: "E", row: i)).Equals("")) {
              CreateSubject(2021, helper.Get(column: "B", row: i), helper.Get(column: "H", row: i),
                  CheckValueExcel(helper.Get(column: "E", row: i)));
            } else if (!CheckValueExcel(helper.Get(column: "F", row: i)).Equals("")) {
              CreateSubject(2021, helper.Get(column: "B", row: i), helper.Get(column: "H", row: i),
                  CheckValueExcel(helper.Get(column: "F", row: i)));
            }
          }
        }
      } catch (Exception ex) { Console.WriteLine(ex.Message); }
    }

    private static void CreateDisciplineProgram(string subject_id, int year, string subject_content, string subject_results) {
      try {
        DB = new SQLiteConnection(pathDB);
        DB.Open();
        SQLiteCommand regcmd = DB.CreateCommand();
        regcmd.CommandText = "INSERT INTO DisciplineProgram VALUES(@subject_id, @year, @subject_content, @subject_results)";
        regcmd.Parameters.AddWithValue("@subject_id", subject_id);
        regcmd.Parameters.AddWithValue("@year", year);
        regcmd.Parameters.AddWithValue("@subject_content", subject_content);
        regcmd.Parameters.AddWithValue("@subject_results", subject_results);
        regcmd.ExecuteNonQuery();
        DB.Close();
      } catch (Exception ex) {
        Console.WriteLine("Error: " + ex);
      }
    }

    private static void CreateQuestion(string subject_id, int year, string question_name, string option_1, string option_2, string option_3, string option_4, string correct_answer) {
      try {
        DB = new SQLiteConnection(pathDB);
        DB.Open();
        SQLiteCommand regcmd = DB.CreateCommand();
        regcmd.CommandText = "INSERT INTO Questions VALUES(NULL, @subject_id, @year, @question_name, @option_1, @option_2, @option_3, @option_4, @correct_answer)";
        regcmd.Parameters.AddWithValue("@subject_id", subject_id);
        regcmd.Parameters.AddWithValue("@year", year);
        regcmd.Parameters.AddWithValue("@question_name", question_name);
        regcmd.Parameters.AddWithValue("@option_1", option_1);
        regcmd.Parameters.AddWithValue("@option_2", option_2);
        regcmd.Parameters.AddWithValue("@option_3", option_3);
        regcmd.Parameters.AddWithValue("@option_4", option_4);
        regcmd.Parameters.AddWithValue("@correct_answer", correct_answer);
        regcmd.ExecuteNonQuery();
        DB.Close();
      } catch (Exception ex) {
        Console.WriteLine("Error: " + ex);
      }
    }

    private static void CreateSubject(int year, string subject_name, string subject_hours, string subject_course) {
      try {
        DB = new SQLiteConnection(pathDB);
        DB.Open();
        SQLiteCommand regcmd = DB.CreateCommand();
        regcmd.CommandText = "INSERT INTO Subjects VALUES(NULL, @year, @subject_name, @subject_hours, @subject_course)";
        regcmd.Parameters.AddWithValue("@year", year);
        regcmd.Parameters.AddWithValue("@subject_name", subject_name);
        regcmd.Parameters.AddWithValue("@subject_hours", subject_hours);
        regcmd.Parameters.AddWithValue("@subject_course", subject_course);
        regcmd.ExecuteNonQuery();
        DB.Close();
      } catch (Exception ex) {
        Console.WriteLine("Error: " + ex);
      }
    }

    private static void DeleteDeprecatedData(string table) {
      try {
        DB = new SQLiteConnection(pathDB);
        DB.Open();
        SQLiteCommand regcmd = DB.CreateCommand();
        if (table.Equals("Subjects")) {
          regcmd.CommandText = "DELETE FROM Subjects";
        } else if (table.Equals("Questions")) {
          regcmd.CommandText = "DELETE FROM Questions";
        } else {
          regcmd.CommandText = "DELETE FROM DisciplineProgram";
        }
        regcmd.ExecuteNonQuery();
        DB.Close();
      } catch (Exception ex) {
        Console.WriteLine("Error: " + ex);
      }
    }

    private static void CreateUser(string chat_id, string username, string course, string college, string specialty) {
      try {
        DB = new SQLiteConnection(pathDB);
        DB.Open();
        SQLiteCommand regcmd = DB.CreateCommand();
        regcmd.CommandText = "INSERT INTO RegUsers VALUES(@chat_id, @username, @course, @college, @specialty, @admin)";
        regcmd.Parameters.AddWithValue("@chat_id", chat_id);
        regcmd.Parameters.AddWithValue("@username", username);
        regcmd.Parameters.AddWithValue("@course", course);
        regcmd.Parameters.AddWithValue("@college", college);
        regcmd.Parameters.AddWithValue("@specialty", specialty);
        regcmd.Parameters.AddWithValue("@admin", false);
        regcmd.ExecuteNonQuery();
        DB.Close();
      } catch (Exception ex) {
        Console.WriteLine("Error: " + ex);
      }
    }

    private static void RemoveThirdCourseFromPoints(string chat_id) {
      try {
        DB = new SQLiteConnection(pathDB);
        DB.Open();
        SQLiteCommand regcmd = DB.CreateCommand();
        regcmd.CommandText = "DELETE FROM Points WHERE chat_id = @chat_id AND subject_id IN (SELECT subject_id FROM Subjects WHERE subject_course = 3)";
        regcmd.Parameters.AddWithValue("@chat_id", chat_id);
        regcmd.ExecuteNonQuery();
        DB.Close();
      } catch (Exception ex) {
        Console.WriteLine("Error: " + ex);
      }
    }

    private static void RemoveAllPoints() {
      try {
        DB = new SQLiteConnection(pathDB);
        DB.Open();
        SQLiteCommand regcmd = DB.CreateCommand();
        regcmd.CommandText = "DELETE FROM Points";
        regcmd.ExecuteNonQuery();
        DB.Close();
      } catch (Exception ex) {
        Console.WriteLine("Error: " + ex);
      }
    }

    private static void RegisterSubjectInPoints(string chat_id, string subject_id, int result) {
      try {
        DateTime time = DateTime.Now;
        DB = new SQLiteConnection(pathDB);
        DB.Open();
        SQLiteCommand regcmd = DB.CreateCommand();
        regcmd.CommandText = "INSERT INTO Points VALUES(NULL, @chat_id, @subject_id, @result, @time)";
        regcmd.Parameters.AddWithValue("@chat_id", chat_id);
        regcmd.Parameters.AddWithValue("@subject_id", subject_id);
        regcmd.Parameters.AddWithValue("@result", result);
        regcmd.Parameters.AddWithValue("@time", time);
        regcmd.ExecuteNonQuery();
        DB.Close();
      } catch (Exception ex) {
        Console.WriteLine("Error: " + ex);
      }
    }

    private static List<string> GetDisciplineProgram(string subject_id) {
      List<string> currentDisciplineProgram = new();
      try {
        DB = new SQLiteConnection(pathDB);
        DB.Open();
        SQLiteCommand regcmd = DB.CreateCommand();
        regcmd.CommandText = "SELECT subject_content, subject_results FROM DisciplineProgram WHERE subject_id = @subject_id";
        regcmd.Parameters.AddWithValue("@subject_id", subject_id);
        SQLiteDataReader dataReader = regcmd.ExecuteReader();
        while (dataReader.Read()) {
          currentDisciplineProgram.Add(String.Format("{0}", dataReader[0]));
          currentDisciplineProgram.Add(String.Format("{0}", dataReader[1]));
        }
        DB.Close();
      } catch (Exception ex) {
        Console.WriteLine("Error: " + ex);
      }
      return currentDisciplineProgram;
    }

    private static string GetSubjectIDByName(string subject_name) {
      string subject_id = "";
      try {
        DB = new SQLiteConnection(pathDB);
        DB.Open();
        SQLiteCommand regcmd = DB.CreateCommand();
        regcmd.CommandText = "SELECT subject_id FROM Subjects WHERE subject_name = @subject_name";
        regcmd.Parameters.AddWithValue("@subject_name", subject_name);
        SQLiteDataReader dataReader = regcmd.ExecuteReader();
        while (dataReader.Read()) {
          subject_id = String.Format("{0}", dataReader[0]);
        }
        DB.Close();
      } catch (Exception ex) {
        Console.WriteLine("Error: " + ex);
      }
      return subject_id;
    }

    private static string GetSubjectNameById(string subject_id) {
      string subject_name = "";
      try {
        DB = new SQLiteConnection(pathDB);
        DB.Open();
        SQLiteCommand regcmd = DB.CreateCommand();
        regcmd.CommandText = "SELECT subject_name FROM Subjects WHERE subject_id = @subject_id";
        regcmd.Parameters.AddWithValue("@subject_id", subject_id);
        SQLiteDataReader dataReader = regcmd.ExecuteReader();
        while (dataReader.Read()) {
          subject_name = String.Format("{0}", dataReader[0]);
        }
        DB.Close();
      } catch (Exception ex) {
        Console.WriteLine("Error: " + ex);
      }
      return subject_name;
    }

    private static List<string> GetNotPassedSubjects(string chat_id) {
      List<string> subjectList = new();
      try {
        DB = new SQLiteConnection(pathDB);
        DB.Open();
        SQLiteCommand regcmd = DB.CreateCommand();
        regcmd.CommandText = "SELECT subject_name FROM Subjects WHERE subject_id IN (SELECT subject_id FROM Points WHERE chat_id = @chat_id AND result != 3)";
        regcmd.Parameters.AddWithValue("@chat_id", chat_id);
        SQLiteDataReader dataReader = regcmd.ExecuteReader();
        while (dataReader.Read()) {
          subjectList.Add(String.Format("{0}", dataReader[0]));
        }
        DB.Close();
      } catch (Exception ex) {
        Console.WriteLine("Error: " + ex);
      }
      return subjectList;
    }

    private static List<Subject> GiveThirdCourseSubjectsList() {
      try {
        var allSubjectsList = new List<Subject>();
        int year = DateTime.Now.Year;
        DB = new SQLiteConnection(pathDB);
        DB.Open();
        SQLiteCommand regcmd = DB.CreateCommand();
        regcmd.CommandText = "SELECT * FROM Subjects WHERE year = @year AND subject_course = 3";
        regcmd.Parameters.AddWithValue("@year", year);
        SQLiteDataReader dataReader = regcmd.ExecuteReader();
        while (dataReader.Read()) {
          Subject subject = new(String.Format("{0}", dataReader[0]),
                                String.Format("{0}", dataReader[1]),
                                String.Format("{0}", dataReader[2]),
                                String.Format("{0}", dataReader[3]),
                                String.Format("{0}", dataReader[4]));
          allSubjectsList.Add(subject);
        }
        DB.Close();
        return allSubjectsList;
      } catch (Exception ex) {
        Console.WriteLine("Error: " + ex);
        return null;
      }
    }    

    private static List<Subject> GetAllSubjects() {
      try {
        var allSubjectsList = new List<Subject>();
        int year = DateTime.Now.Year;
        DB = new SQLiteConnection(pathDB);
        DB.Open();
        SQLiteCommand regcmd = DB.CreateCommand();
        regcmd.CommandText = "SELECT * FROM Subjects WHERE year = @year";
        regcmd.Parameters.AddWithValue("@year", year);
        SQLiteDataReader dataReader = regcmd.ExecuteReader();
        while (dataReader.Read()) {
          Subject subject = new(String.Format("{0}", dataReader[0]),
                                String.Format("{0}", dataReader[1]),
                                String.Format("{0}", dataReader[2]),
                                String.Format("{0}", dataReader[3]),
                                String.Format("{0}", dataReader[4]));
          allSubjectsList.Add(subject);
        }
        DB.Close();
        return allSubjectsList;
      } catch (Exception ex) {
        Console.WriteLine("Error: " + ex);
        return null;
      }
    }

    private static List<Question> GetQuestionsForSubjectBySubjectID(string subject_id) {
      try {
        var questionsList = new List<Question>();
        DB = new SQLiteConnection(pathDB);
        DB.Open();
        SQLiteCommand regcmd = DB.CreateCommand();
        regcmd.CommandText = "SELECT * FROM Questions WHERE subject_id = @subject_id";
        regcmd.Parameters.AddWithValue("@subject_id", subject_id);
        SQLiteDataReader dataReader = regcmd.ExecuteReader();
        while (dataReader.Read()) {
          Question question = new(String.Format("{0}", dataReader[0]),
                                  String.Format("{0}", dataReader[1]),
                                  String.Format("{0}", dataReader[2]),
                                  String.Format("{0}", dataReader[3]),
                                  String.Format("{0}", dataReader[4]),
                                  String.Format("{0}", dataReader[5]),
                                  String.Format("{0}", dataReader[6]),
                                  String.Format("{0}", dataReader[7]),
                                  String.Format("{0}", dataReader[8]));
          questionsList.Add(question);
        }
        DB.Close();
        return questionsList;
      } catch (Exception ex) {
        Console.WriteLine("Error: " + ex);
        return null;
      }
    }    

    private static List<Points> GetAllPoints(string chat_id) {
      try {
        var allPointsList = new List<Points>();
        DB = new SQLiteConnection(pathDB);
        DB.Open();
        SQLiteCommand regcmd = DB.CreateCommand();
        regcmd.CommandText = "SELECT * FROM Points WHERE chat_id = @chat_id";
        regcmd.Parameters.AddWithValue("@chat_id", chat_id);
        SQLiteDataReader dataReader = regcmd.ExecuteReader();
        while (dataReader.Read()) {
          Points point = new(String.Format("{0}", dataReader[0]),
                             String.Format("{0}", dataReader[1]),
                             String.Format("{0}", dataReader[2]),
                             String.Format("{0}", dataReader[3]),
                             String.Format("{0}", dataReader[4]));
          allPointsList.Add(point);
        }
        DB.Close();
        return allPointsList;
      } catch (Exception ex) {
        Console.WriteLine("Error: " + ex);
        return null;
      }
    }

    private static List<string> GetAllUsersID() {      
      try {
        var usersIDList = new List<string>();
        DB = new SQLiteConnection(pathDB);
        DB.Open();
        SQLiteCommand regcmd = DB.CreateCommand();
        regcmd.CommandText = "SELECT chat_id FROM RegUsers";
        SQLiteDataReader dataReader = regcmd.ExecuteReader();
        while (dataReader.Read()) {
          usersIDList.Add(String.Format("{0}", dataReader[0]));
        }
        DB.Close();
        return usersIDList;
      } catch (Exception ex) {
        Console.WriteLine("Error: " + ex);
        return null;
      }
    }

    private static bool IsRegisteredAlready(string chat_id) {
      bool isRegistered = false;
      try {
        DB = new SQLiteConnection(pathDB);
        DB.Open();
        SQLiteCommand regcmd = DB.CreateCommand();
        regcmd.CommandText = "SELECT username FROM RegUsers WHERE chat_id = @chat_id";
        regcmd.Parameters.AddWithValue("@chat_id", chat_id);
        SQLiteDataReader dataReader = regcmd.ExecuteReader();
        while (dataReader.Read()) {
          if (string.Format("{0}", dataReader[0]) != null) {
            isRegistered = true;
          }
        }
        DB.Close();
      } catch (Exception ex) {
        Console.WriteLine("Error: " + ex);
      }
      return isRegistered;
    }

    private static bool GetAdmin(string chat_id) {
      bool admin = false;
      try {
        DB = new SQLiteConnection(pathDB);
        DB.Open();
        SQLiteCommand regcmd = DB.CreateCommand();
        regcmd.CommandText = "SELECT admin FROM RegUsers WHERE chat_id = @chat_id";
        regcmd.Parameters.AddWithValue("@chat_id", chat_id);
        SQLiteDataReader dataReader = regcmd.ExecuteReader();
        while (dataReader.Read()) {
          admin = bool.Parse(string.Format("{0}", dataReader[0]));
        }
        DB.Close();
      } catch (Exception ex) {
        Console.WriteLine("Error: " + ex);
      }
      return admin;
    }

    private static string GetSpecialty(string chat_id) {
      string specialty = "";
      try {
        DB = new SQLiteConnection(pathDB);
        DB.Open();
        SQLiteCommand regcmd = DB.CreateCommand();
        regcmd.CommandText = "SELECT specialty FROM RegUsers WHERE chat_id = @chat_id";
        regcmd.Parameters.AddWithValue("@chat_id", chat_id);
        SQLiteDataReader dataReader = regcmd.ExecuteReader();
        while (dataReader.Read()) {
          specialty = string.Format("{0}", dataReader[0]);
        }
        DB.Close();
      } catch (Exception ex) {
        Console.WriteLine("Error: " + ex);
      }
      return specialty;
    }

    private static void SetSpecialty(string chat_id, string specialty) {
      try {
        DB = new SQLiteConnection(pathDB);
        DB.Open();
        SQLiteCommand regcmd = DB.CreateCommand();
        regcmd.CommandText = "UPDATE RegUsers SET specialty = @specialty WHERE chat_id = @chat_id";
        regcmd.Parameters.AddWithValue("@specialty", specialty);
        regcmd.Parameters.AddWithValue("@chat_id", chat_id);
        regcmd.ExecuteNonQuery();
        DB.Close();
      } catch (Exception ex) {
        Console.WriteLine("Error: " + ex);
      }
    }

    private static string GetCourse(string chat_id) {
      string course = "";
      try {
        DB = new SQLiteConnection(pathDB);
        DB.Open();
        SQLiteCommand regcmd = DB.CreateCommand();
        regcmd.CommandText = "SELECT course FROM RegUsers WHERE chat_id = @chat_id";
        regcmd.Parameters.AddWithValue("@chat_id", chat_id);
        SQLiteDataReader dataReader = regcmd.ExecuteReader();
        while (dataReader.Read()) {
          course = string.Format("{0}", dataReader[0]);
        }
        DB.Close();
      } catch (Exception ex) {
        Console.WriteLine("Error: " + ex);
      }
      return course;
    }

    private static void SetCourse(string chat_id, string course) {
      try {
        DB = new SQLiteConnection(pathDB);
        DB.Open();
        SQLiteCommand regcmd = DB.CreateCommand();
        regcmd.CommandText = "UPDATE RegUsers SET course = @course WHERE chat_id = @chat_id";
        regcmd.Parameters.AddWithValue("@course", course);
        regcmd.Parameters.AddWithValue("@chat_id", chat_id);
        regcmd.ExecuteNonQuery();
        DB.Close();
      } catch (Exception ex) {
        Console.WriteLine("Error: " + ex);
      }
    }

    private static string GetCollege(string chat_id) {
      string college = "";
      try {
        DB = new SQLiteConnection(pathDB);
        DB.Open();
        SQLiteCommand regcmd = DB.CreateCommand();
        regcmd.CommandText = "SELECT college FROM RegUsers WHERE chat_id = @chat_id";
        regcmd.Parameters.AddWithValue("@chat_id", chat_id);
        SQLiteDataReader dataReader = regcmd.ExecuteReader();
        while (dataReader.Read()) {
          college = string.Format("{0}", dataReader[0]);
        }
        DB.Close();
      } catch (Exception ex) {
        Console.WriteLine("Error: " + ex);
      }
      return college;
    }

    private static void SetCollege(string chat_id, string college) {
      try {
        DB = new SQLiteConnection(pathDB);
        DB.Open();
        SQLiteCommand regcmd = DB.CreateCommand();
        regcmd.CommandText = "UPDATE RegUsers SET college = @college WHERE chat_id = @chat_id";
        regcmd.Parameters.AddWithValue("@college", college);
        regcmd.Parameters.AddWithValue("@chat_id", chat_id);
        regcmd.ExecuteNonQuery();
        DB.Close();
      } catch (Exception ex) {
        Console.WriteLine("Error: " + ex);
      }
    }

    private static void EnterNewResultForCurrentSubject(string result, string subject_id) {
      try {
        DB = new SQLiteConnection(pathDB);
        DB.Open();
        SQLiteCommand regcmd = DB.CreateCommand();
        regcmd.CommandText = "UPDATE Points SET result = @result WHERE subject_id = @subject_id";
        regcmd.Parameters.AddWithValue("@result", result);
        regcmd.Parameters.AddWithValue("@subject_id", subject_id);
        regcmd.ExecuteNonQuery();
        DB.Close();
      } catch (Exception ex) {
        Console.WriteLine("Error: " + ex);
      }
    }

    private static void MakeUserAdmin(string chat_id) {
      try {
        DB = new SQLiteConnection(pathDB);
        DB.Open();
        SQLiteCommand regcmd = DB.CreateCommand();
        regcmd.CommandText = "UPDATE RegUsers SET admin = true WHERE chat_id = @chat_id";
        regcmd.Parameters.AddWithValue("@chat_id", chat_id);
        regcmd.ExecuteNonQuery();
        DB.Close();
      } catch (Exception ex) {
        Console.WriteLine("Error: " + ex);
      }
    }

    private static void MakeUserNotAdmin(string chat_id) {
      try {
        DB = new SQLiteConnection(pathDB);
        DB.Open();
        SQLiteCommand regcmd = DB.CreateCommand();
        regcmd.CommandText = "UPDATE RegUsers SET admin = false WHERE chat_id = @chat_id";
        regcmd.Parameters.AddWithValue("@chat_id", chat_id);
        regcmd.ExecuteNonQuery();
        DB.Close();
      } catch (Exception ex) {
        Console.WriteLine("Error: " + ex);
      }
    }
  }
}