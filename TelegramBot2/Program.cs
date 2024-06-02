using System;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Data.SqlClient;

namespace TelegramBot2
{
    class Program
    {
        private static TelegramBotClient botClient;

        static void Main(string[] args)
        {
            botClient = new TelegramBotClient("6672189980:AAFuXdaWjfTY5m-J9OfFNP-PaP3qfpYToXw");
            botClient.OnMessage += Bot_OnMessage;
            botClient.StartReceiving();


            Console.WriteLine("Бот запущен. Нажмите Enter для остановки.");
            Console.ReadLine();

            botClient.StopReceiving();
        }

        private static async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            if (e.Message.Type == MessageType.Text)
            {
                string messageText = e.Message.Text;

                if (messageText.StartsWith("/start"))
                {
                    string replyText = "Добро пожаловать в бота по заявкам на починку!\n\n" +
                        "Пожалуйста, укажите ФИО подающего заявку (Иванов Михаил Сергеевич), название устройства(Lenovo Gen 7), кабинет(102)\n\n" +
                        "Пример: Иванов Михаил Сергеевич, Lenovo Gen 7, 102";

                    await botClient.SendTextMessageAsync(
                        chatId: e.Message.Chat,
                        text: replyText
                    );
                }
                else
                {
                    string[] requestData = messageText.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (requestData.Length == 3)
                    {
                        string fio = requestData[0].Trim();
                        string deviceNaz = requestData[1].Trim();
                        string mesto = requestData[2].Trim();

                        string requestText = "Заявка на починку:\n\n" + "ФИО: " + fio + "\n" + "Название устройства: " + deviceNaz + "\n" + "Кабинет: " + mesto/* + "\n" +"Срочность: " + urgency*/;

                        // Отправка заявки в указанный чат
                        await botClient.SendTextMessageAsync(
                            chatId: "-1001859381557",
                            text: requestText
                        );

                        string replyText = "Ваша заявка была успешно отправлена.";
                        //адрес для подключение бд
                        string connectionString = @"Data Source=DESKTOP-GV2J6AJ;Initial Catalog=Inventarizauia3;Integrated Security=True";

                        // Подключение к базе данных
                        using (SqlConnection connection = new SqlConnection(connectionString))
                        {
                            connection.Open();

                            // Получение идентификатора сотрудника по его ФИО
                            string selectSotrudnikiIdQuery = "SELECT sotrudnikiId FROM Sotrudniki WHERE fio = @fio";
                            SqlCommand selectSotrudnikiIdCommand = new SqlCommand(selectSotrudnikiIdQuery, connection);
                            selectSotrudnikiIdCommand.Parameters.AddWithValue("@fio", fio);
                            int sotrudnikiId = (int)selectSotrudnikiIdCommand.ExecuteScalar();

                            // Получение идентификатора оборудования по его названию
                            string selectOborudovanieIdQuery = "SELECT oborudovanieId FROM Oborudovanie WHERE title = @title";
                            SqlCommand selectOborudovanieIdCommand = new SqlCommand(selectOborudovanieIdQuery, connection);
                            selectOborudovanieIdCommand.Parameters.AddWithValue("@title", deviceNaz);
                            int oborudovanieId = (int)selectOborudovanieIdCommand.ExecuteScalar();

                            // Получение идентификатора места установки по его названию
                            string selectMestoYstanovkiIdQuery = "SELECT mestoYstanovkiId FROM MestoYstanovki WHERE title = @title";
                            SqlCommand selectMestoYstanovkiIdCommand = new SqlCommand(selectMestoYstanovkiIdQuery, connection);
                            selectMestoYstanovkiIdCommand.Parameters.AddWithValue("@title", mesto);
                            int mestoYstanovkiId = (int)selectMestoYstanovkiIdCommand.ExecuteScalar();

                            string insertQuery = @"INSERT INTO Zauvka (sotrudnikiId, oborudovanieId, mestoYstanovkiId, data, statusZauvkaId)
                            VALUES (@sotrudnikiId, @oborudovanieId, @mestoYstanovkiId, @data, 
                            (SELECT statusZauvkaId FROM StatusZauvka WHERE title = 'В работе'))";

                            SqlCommand insertCommand = new SqlCommand(insertQuery, connection);
                            insertCommand.Parameters.AddWithValue("@sotrudnikiId", sotrudnikiId);
                            insertCommand.Parameters.AddWithValue("@oborudovanieId", oborudovanieId);
                            insertCommand.Parameters.AddWithValue("@mestoYstanovkiId", mestoYstanovkiId);
                            insertCommand.Parameters.AddWithValue("@data", DateTime.Now);
                            insertCommand.ExecuteNonQuery();

                            // Закрытие подключения к базе данных
                            connection.Close();
                        }

                        await botClient.SendTextMessageAsync(
                            chatId: e.Message.Chat,
                            text: replyText
                        );
                    }
                    else
                    {
                        string replyText = "Некорректный формат заявки. Пожалуйста, укажите ФИО подающего заявку (Иванов Михаил Сергеевич), название устройства(Lenovo Gen 7), кабинет(102)\n\n" +
                        "Пример: Иванов Михаил Сергеевич, Lenovo Gen 7, 102";

                        await botClient.SendTextMessageAsync(
                            chatId: e.Message.Chat,
                            text: replyText
                        );
                    }
                }
            }
        }
    }
}