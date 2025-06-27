using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CybersecurityChatbot
{
    public partial class MainWindow : Window
    {
        private string userName = "";
        private bool gotName = false;
        private bool gotFeeling = false;
        private bool awaitingReminder = false;
        private string pendingTaskTitle = "";
        private List<string> activityLog = new();
        private List<CyberTask> cyberTasks = new();
        private List<QuizQuestion> quizQuestions;
        private int currentQuestionIndex = 0;
        private int quizScore = 0;
        private const string TasksFilePath = "tasks.txt";

        public MainWindow()
        {
            InitializeComponent();
            LoadTasksFromFile();
            DisplayBotMessage("👋 Hello! I'm RELEEY, your Cybersecurity Assistant.");
            DisplayBotMessage("What's your name?");
            LoadQuizQuestions();
        }

        private void UserInputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrEmpty(UserInputTextBox.Text.Trim()))
            {
                SendButton_Click(sender, null);
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string userMessage = UserInputTextBox.Text.Trim();
            if (string.IsNullOrEmpty(userMessage)) return;

            DisplayUserMessage(userMessage);
            string botReply = GetChatbotResponse(userMessage);
            DisplayBotMessage(botReply);
            UserInputTextBox.Clear();
        }

        private void ShowTasks_Click(object sender, RoutedEventArgs e)
        {
            if (cyberTasks.Count == 0)
                DisplayBotMessage("You have no tasks yet.");
            else
                foreach (var task in cyberTasks)
                    DisplayBotMessage(task.ToString());
        }

        private void ClearTasks_Click(object sender, RoutedEventArgs e)
        {
            cyberTasks.Clear();
            SaveTasksToFile();
            DisplayBotMessage("✅ All tasks have been cleared.");
            activityLog.Add("All tasks cleared");
        }

        private void OpenAddTaskPanel_Click(object sender, RoutedEventArgs e)
        {
            AddTaskPanel.Visibility = Visibility.Visible;
            var fadeIn = (Storyboard)FindResource("FadeIn");
            fadeIn.Begin(AddTaskPanel);
        }

        private void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            string title = TitleBox.Text.Trim();
            string desc = DescBox.Text.Trim();

            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show("Please enter a task title.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (title.Length > 100)
            {
                MessageBox.Show("Task title must be 100 characters or less.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newTask = new CyberTask { Title = title, Description = desc };
            if (ReminderDatePicker.SelectedDate.HasValue)
                newTask.ReminderDate = ReminderDatePicker.SelectedDate.Value;

            cyberTasks.Add(newTask);
            SaveTasksToFile();
            DisplayBotMessage($"Task \"{title}\" added!");
            activityLog.Add($"Task added: '{title}'" +
                (newTask.ReminderDate.HasValue ? $" (Reminder set for {newTask.ReminderDate.Value.ToShortDateString()})" : ""));
            ClearTaskInput();
            var fadeOut = (Storyboard)FindResource("FadeOut");
            fadeOut.Completed += (s, args) => AddTaskPanel.Visibility = Visibility.Collapsed;
            fadeOut.Begin(AddTaskPanel);
        }

        private void CancelAddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            ClearTaskInput();
            var fadeOut = (Storyboard)FindResource("FadeOut");
            fadeOut.Completed += (s, args) => AddTaskPanel.Visibility = Visibility.Collapsed;
            fadeOut.Begin(AddTaskPanel);
        }

        private void StartQuiz_Click(object sender, RoutedEventArgs e)
        {
            currentQuestionIndex = 0;
            quizScore = 0;
            QuizPanel.Visibility = Visibility.Visible;
            var fadeIn = (Storyboard)FindResource("FadeIn");
            fadeIn.Begin(QuizPanel);
            DisplayCurrentQuestion();
        }

        private void NextQuestionButton_Click(object sender, RoutedEventArgs e)
        {
            currentQuestionIndex++;
            if (currentQuestionIndex < quizQuestions.Count)
            {
                DisplayCurrentQuestion();
            }
            else
            {
                var fadeOut = (Storyboard)FindResource("FadeOut");
                fadeOut.Completed += (s, args) => QuizPanel.Visibility = Visibility.Collapsed;
                fadeOut.Begin(QuizPanel);
                DisplayBotMessage($"🎯 Quiz finished! You scored {quizScore}/{quizQuestions.Count}.");
                DisplayBotMessage(quizScore >= 8 ? "Great job! You're a cybersecurity pro!" :
                                  quizScore >= 5 ? "Nice work! Keep learning!" : "Keep practicing to stay safe online.");
                activityLog.Add($"Quiz completed: {quizScore}/{quizQuestions.Count} on {DateTime.Now.ToShortDateString()}");
                RetryQuizButton.Visibility = Visibility.Visible;
            }
        }

        private void RetryQuizButton_Click(object sender, RoutedEventArgs e)
        {
            StartQuiz_Click(sender, e);
        }

        private void DisplayUserMessage(string message)
        {
            var textBlock = new TextBlock
            {
                Text = $"🧑 You: {message}",
                Foreground = Brushes.White,
                Style = (Style)FindResource("ChatMessageStyle")
            };
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                CornerRadius = new CornerRadius(5),
                Child = textBlock,
                Margin = new Thickness(10, 5, 10, 5)
            };
            ChatPanel.Children.Add(border);
            ScrollToBottom();
        }

        private void DisplayBotMessage(string message)
        {
            var textBlock = new TextBlock
            {
                Text = $"🤖 RELEEY: {message}",
                Foreground = Brushes.LightGreen,
                Style = (Style)FindResource("ChatMessageStyle")
            };
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(40, 60, 40)),
                CornerRadius = new CornerRadius(5),
                Child = textBlock,
                Margin = new Thickness(10, 5, 10, 5)
            };
            ChatPanel.Children.Add(border);
            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            ((ScrollViewer)ChatPanel.Parent).ScrollToEnd();
        }

        private string GetChatbotResponse(string input)
        {
            string lower = input.ToLower().Trim();

            if (!gotName)
            {
                userName = input;
                gotName = true;
                return $"Nice to meet you, {userName}! How are you feeling today?";
            }

            if (!gotFeeling)
            {
                gotFeeling = true;
                return "Thanks for sharing! Ask me anything about cybersecurity or try the quiz!";
            }

            // Task addition logic
            if (lower.StartsWith("add a task to") || lower.Contains("add task") || lower.Contains("create task") || lower.Contains("set a task"))
            {
                Match match = Regex.Match(input, @"(?:add a task to|add task|create task|set a task)\s+(.+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    string title = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(title))
                    {
                        title = char.ToUpper(title[0]) + (title.Length > 1 ? title.Substring(1) : "");
                        cyberTasks.Add(new CyberTask { Title = title, Description = title });
                        pendingTaskTitle = title;
                        awaitingReminder = true;
                        activityLog.Add($"Task added: '{title}'");
                        SaveTasksToFile();
                        return $"Task added: '{title}'. Would you like to set a reminder for this task?";
                    }
                    else
                    {
                        return "Please specify a task to add.";
                    }
                }
            }

            if (awaitingReminder && lower.Contains("yes"))
            {
                DateTime reminder = DateTime.Now.AddDays(1);
                var task = cyberTasks.FirstOrDefault(t => t.Title == pendingTaskTitle);
                if (task != null)
                {
                    task.ReminderDate = reminder;
                    activityLog.Add($"Reminder set: '{pendingTaskTitle}' for {reminder.ToShortDateString()}");
                    SaveTasksToFile();
                }
                awaitingReminder = false;
                return $"Reminder set for '{pendingTaskTitle}' on tomorrow's date.";
            }

            if (lower.Contains("remind me to"))
            {
                Match match = Regex.Match(lower, @"remind me to (.+)");
                if (match.Success)
                {
                    string remainder = match.Groups[1].Value.Trim();
                    DateTime reminderDate = DateTime.Now;
                    string taskDescription = remainder;

                    Match inDaysMatch = Regex.Match(remainder, @"(.+) in (\d+) days?");
                    if (inDaysMatch.Success)
                    {
                        taskDescription = inDaysMatch.Groups[1].Value.Trim();
                        if (int.TryParse(inDaysMatch.Groups[2].Value, out int days))
                            reminderDate = DateTime.Now.AddDays(days);
                    }
                    else
                    {
                        Match onDateMatch = Regex.Match(remainder, @"(.+) on (\d{4}-\d{2}-\d{2})");
                        if (onDateMatch.Success)
                        {
                            taskDescription = onDateMatch.Groups[1].Value.Trim();
                            if (DateTime.TryParseExact(onDateMatch.Groups[2].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                            {
                                reminderDate = parsedDate;
                            }
                        }
                        else
                        {
                            if (remainder.EndsWith(" tomorrow"))
                            {
                                taskDescription = remainder.Replace(" tomorrow", "").Trim();
                                reminderDate = DateTime.Now.AddDays(1);
                            }
                            else
                            {
                                Match nextDayMatch = Regex.Match(remainder, @"(.+) next (monday|tuesday|wednesday|thursday|friday|saturday|sunday)");
                                if (nextDayMatch.Success)
                                {
                                    taskDescription = nextDayMatch.Groups[1].Value.Trim();
                                    string dayOfWeekStr = nextDayMatch.Groups[2].Value;
                                    if (Enum.TryParse<DayOfWeek>(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dayOfWeekStr), out DayOfWeek desiredDay))
                                    {
                                        reminderDate = GetNextWeekday(DateTime.Now, desiredDay);
                                    }
                                }
                            }
                        }
                    }

                    taskDescription = char.ToUpper(taskDescription[0]) + (taskDescription.Length > 1 ? taskDescription.Substring(1) : "");
                    cyberTasks.Add(new CyberTask { Title = taskDescription, Description = taskDescription, ReminderDate = reminderDate });
                    activityLog.Add($"NLP-triggered reminder: '{taskDescription}' for {reminderDate.ToShortDateString()}");
                    SaveTasksToFile();
                    return $"Reminder set for '{taskDescription}' on {reminderDate:dddd, MMM d, yyyy}.";
                }
            }

            if (lower.Contains("activity log") || lower.Contains("what have you done"))
            {
                if (activityLog.Count == 0)
                    return "I haven’t done anything yet.";
                var recent = activityLog.TakeLast(5).Reverse().ToList();
                string response = "Here's a summary of recent actions:\n";
                for (int i = 0; i < recent.Count; i++)
                    response += $"{i + 1}. {recent[i]}\n";
                return response.Trim();
            }

            if (lower.Contains("phishing"))
                return "Phishing is a scam to trick you into revealing personal info. Always verify email senders.";
            if (lower.Contains("password"))
                return "Use strong, unique passwords for each account. Consider a password manager.";
            if (lower.Contains("privacy"))
                return "Adjust app permissions and be mindful of what you share online.";
            if (lower.Contains("malware"))
                return "Malware is malicious software that can damage your device or steal data. Use antivirus software.";
            if (lower.Contains("two-factor") || lower.Contains("2fa"))
                return "Two-factor authentication adds an extra layer of security beyond just a password.";
            if (lower.Contains("social engineering"))
                return "Social engineering tricks people into revealing confidential info. Always verify identities.";
            if (lower.Contains("updates") || lower.Contains("patches"))
                return "Keep your software updated to protect against security vulnerabilities.";

            if (lower.EndsWith("?") || lower.StartsWith("what") || lower.StartsWith("how") || lower.StartsWith("why"))
                return "Great question! Visit https://www.cyberaware.gov.za for tips.";

            return "I'm here to help with cybersecurity, tasks, and reminders!";
        }

        private DateTime GetNextWeekday(DateTime from, DayOfWeek day)
        {
            int start = (int)from.DayOfWeek;
            int target = (int)day;
            int daysToAdd = (target <= start) ? 7 - (start - target) : target - start;
            return from.AddDays(daysToAdd);
        }

        private void ClearTaskInput()
        {
            TitleBox.Text = "";
            DescBox.Text = "";
            ReminderDatePicker.SelectedDate = null;
        }

        private void LoadTasksFromFile()
        {
            if (File.Exists(TasksFilePath))
            {
                var lines = File.ReadAllLines(TasksFilePath);
                foreach (var line in lines)
                {
                    var parts = line.Split('|');
                    if (parts.Length >= 2)
                    {
                        var task = new CyberTask { Title = parts[0], Description = parts[1] };
                        if (parts.Length == 3 && DateTime.TryParse(parts[2], out var date))
                            task.ReminderDate = date;
                        cyberTasks.Add(task);
                    }
                }
            }
        }

        private void SaveTasksToFile()
        {
            var lines = cyberTasks.Select(t => $"{t.Title}|{t.Description}|{(t.ReminderDate.HasValue ? t.ReminderDate.Value.ToString("yyyy-MM-dd") : "")}");
            File.WriteAllLines(TasksFilePath, lines);
        }

        private void LoadQuizQuestions()
        {
            quizQuestions = new List<QuizQuestion>
            {
                new("What should you do if you receive an email asking for your password?", new[] { "Reply", "Delete", "Report as phishing", "Ignore" }, 2, "Reporting phishing helps prevent scams."),
                new("True or False: Use the same password everywhere.", new[] { "True", "False" }, 1, "Always use unique passwords."),
                new("Which is a strong password?", new[] { "123456", "password", "Qw!9z$Lk@", "letmein" }, 2, "Use complex passwords."),
                new("What is phishing?", new[] { "A virus", "Tricking users via email", "Backup strategy", "Firewall" }, 1, "It’s a form of fraud."),
                new("Clicking unknown links is safe.", new[] { "True", "False" }, 1, "Avoid unknown links."),
                new("What is social engineering?", new[] { "Software hacking", "Manipulating people", "Social media", "None" }, 1, "It exploits human trust."),
                new("Sharing birthday is safe online.", new[] { "True", "False" }, 1, "Personal details can be misused."),
                new("What's 2FA?", new[] { "Second password", "Two-step verification", "App lock", "Firewall" }, 1, "2FA boosts account security."),
                new("Do you still need antivirus software?", new[] { "Yes", "No" }, 0, "It helps stop malware."),
                new("Best practice for public Wi-Fi?", new[] { "No VPN", "Use VPN", "Share login", "Nothing" }, 1, "Always use a VPN.")
            };
        }

        private void DisplayCurrentQuestion()
        {
            var q = quizQuestions[currentQuestionIndex];
            QuizQuestionText.Text = $"Question {currentQuestionIndex + 1} of {quizQuestions.Count}: {q.Text}";
            QuizFeedbackText.Text = "";
            NextQuestionButton.Visibility = Visibility.Collapsed;
            QuizOptionsPanel.Children.Clear();

            for (int i = 0; i < q.Options.Length; i++)
            {
                int index = i;
                Button option = new()
                {
                    Content = q.Options[i],
                    Tag = index,
                    Margin = new Thickness(0, 5, 0, 0),
                    Width = 500,
                    FontSize = 14,
                    Background = new SolidColorBrush(Color.FromRgb(50, 50, 50))
                };
                option.Click += (s, e) => CheckAnswer(index);
                QuizOptionsPanel.Children.Add(option);
            }
        }

        private void CheckAnswer(int selectedIndex)
        {
            var q = quizQuestions[currentQuestionIndex];
            foreach (Button btn in QuizOptionsPanel.Children)
            {
                btn.IsEnabled = false;
                int btnIndex = (int)btn.Tag;
                if (btnIndex == q.CorrectOption)
                    btn.Background = Brushes.LightGreen;
                else if (btnIndex == selectedIndex)
                    btn.Background = Brushes.OrangeRed;
            }

            if (selectedIndex == q.CorrectOption)
            {
                quizScore++;
                QuizFeedbackText.Foreground = Brushes.LightGreen;
                QuizFeedbackText.Text = "✅ Correct! " + q.Feedback;
            }
            else
            {
                QuizFeedbackText.Foreground = Brushes.OrangeRed;
                QuizFeedbackText.Text = $"❌ Incorrect! Correct answer: \"{q.Options[q.CorrectOption]}\". {q.Feedback}";
            }

            NextQuestionButton.Visibility = Visibility.Visible;
        }
    }

    public class CyberTask
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime? ReminderDate { get; set; }
        public override string ToString() => $"{Title}: {Description}" +
            (ReminderDate.HasValue ? $" (Reminder: {ReminderDate.Value.ToShortDateString()})" : "");
    }

    public class QuizQuestion
    {
        public string Text { get; }
        public string[] Options { get; }
        public int CorrectOption { get; }
        public string Feedback { get; }

        public QuizQuestion(string text, string[] options, int correctOption, string feedback)
        {
            Text = text;
            Options = options;
            CorrectOption = correctOption;
            Feedback = feedback;
        }
    }
}