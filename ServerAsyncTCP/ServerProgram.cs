using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Transactions;

var tcpListener = new TcpListener(IPAddress.Any, 8888);
//Список активных подключенных клиентов
Dictionary<TcpClient, string> clients = new Dictionary<TcpClient, string>();
//Сообщения пользователей(не учитывается уникальность)
List<KeyValuePair<string, string>> messages = new List<KeyValuePair<string, string>>();

try
{
	// запускаем сервер
	tcpListener.Start();
	Console.WriteLine("Сервер запущен. Ожидание подключений...");

	while (true)
	{
		// получаем подключение в виде TcpClient
		var tcpClient = await tcpListener.AcceptTcpClientAsync();
		//// создаем новую задачу для обслуживания нового клиента
		//Task.Run(async () => await ProcessClientAsync(tcpClient));

		// вместо задач можно использовать стандартный Thread
		Thread thr = new Thread(async () => await ProcessClientAsync(tcpClient));
		thr.IsBackground = true;
		thr.Start();
	}
}
finally
{
	tcpListener.Stop();
}

// обрабатываем запрос клиента
async Task ProcessClientAsync(TcpClient tcpClient)
{
	try
	{
		//Получаем поток данных
		NetworkStream stream = tcpClient.GetStream();

		// буфер для входящих данных
		var response = new List<byte>();
		int bytesRead = 10;

		//Получение имени клиента и связывание его в словаре
		while ((bytesRead = stream.ReadByte()) != '\0')
		{
			// добавляем в буфер
			response.Add((byte)bytesRead);
		}
		var name = Encoding.UTF8.GetString(response.ToArray());

		clients.Add(tcpClient, name);
		await SendClients(stream);
		await Console.Out.WriteLineAsync($"Клиент с именем {name} и адресом {tcpClient.Client.RemoteEndPoint} подключился");		
		response.Clear();

		//Данные о подключенных клиентах
		Console.WriteLine("Сейчас подключены:");
		if (clients.Count > 0)
		foreach (var item in clients)
		{
			await Console.Out.WriteLineAsync($"Клиент {item.Value} с адресом {item.Key.Client.RemoteEndPoint}");
		}


		while (true)
		{

			// считываем данные до конечного символа
			while ((bytesRead = stream.ReadByte()) != '\0')
			{
				// добавляем в буфер
				response.Add((byte)bytesRead);
			}
			var input = Encoding.UTF8.GetString(response.ToArray());

			var arr = input.Split('\n');
			string message = arr[0];
			string AdressatName = arr[1];

			//Сообщения от клиента
			string tempName;
			clients.TryGetValue(tcpClient, out tempName);

			//Получение всех сообщений для данного клиента
			if (message == "GETM")
			{
				Console.WriteLine($"Всего сообщений на сервере {messages.Count}" );
				if (messages.Count > 0)
				{
					StringBuilder t = new StringBuilder($"Сообщения для клиента {tempName}: \n") ;
					foreach (var item in messages)
					{
						Console.WriteLine($"Имя {item.Key} Сообщение {item.Value}");
						if (item.Key == tempName)
						{
							t.Append(item.Value + '\n');
						}
					}
					await stream.WriteAsync(Encoding.UTF8.GetBytes(t.ToString() + '\0'));

					//await stream.Socket.SendAsync(Encoding.UTF8.GetBytes(t.ToString() + '\0'));

					//await Console.Out.WriteLineAsync($"Результирующая строка: \n{t}") ;
					//var arr = Encoding.UTF8.GetBytes(t.ToString());
					//await Console.Out.WriteLineAsync($"Результирующая строка в байтах");
					//foreach (var item in arr)
					//{
					//	Console.WriteLine(item);
					//}

				}
				await stream.WriteAsync(Encoding.UTF8.GetBytes("Нет сообщений" + '\0'));
				//await stream.Socket.SendAsync(Encoding.UTF8.GetBytes("Нет сообщений" + '\0'));
			}
			else if(message == "GETC")
			{
				await SendClients(stream);
			}
			else if(message == "END")
			{
				break;
			}
			else
			{
				messages.Add(new KeyValuePair<string, string> (AdressatName, message));
			}


			//Служебная информация
			await Console.Out.WriteLineAsync($"{tempName}: {message}") ;

			response.Clear();
		}
		await Console.Out.WriteLineAsync($"Клиент  {tcpClient.Client.RemoteEndPoint} прервал соединение");
		if (clients.Keys.Contains(tcpClient)) { clients.Remove(tcpClient); }
		tcpClient.Close();
	}
	catch (Exception)
	{
		await Console.Out.WriteLineAsync($"Клиент {tcpClient.Client.RemoteEndPoint} аварийно отключился");
		if (clients.Keys.Contains(tcpClient)) { clients.Remove(tcpClient); }
		tcpClient.Close();
	}


	async Task SendClients(NetworkStream _stream)
	{
		int pos = 0;
		string resString = $"{pos} - Всем;";
		foreach (var client in clients)
		{
			pos++;
			resString += $"{pos} - {client.Value};";
		}
		resString += "\n";
		await _stream.WriteAsync(Encoding.UTF8.GetBytes(resString));
	}
}