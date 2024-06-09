using System.ComponentModel;
using System.Net.Sockets;
using System.Text;


List<string> names = new List<string>();

using TcpClient tcpClient = new TcpClient();
await tcpClient.ConnectAsync("127.0.0.1", 8888);
var stream = tcpClient.GetStream();

// буфер для входящих данных
var response = new List<byte>();
// для считывания байтов из потока
int bytesRead = 10;

Console.WriteLine("Введите имя клиента");
string message = Console.ReadLine();

byte[] data = Encoding.UTF8.GetBytes(message + '\0');
await stream.WriteAsync(data);
names = await GetClientsAsync();

do
{
	Console.WriteLine("Кому вы хотите оставить сообщение?");
	foreach (string name in names)
	{
		Console.WriteLine(name);
	}
	message = Console.ReadLine();
	string preName;
	if (int.TryParse(message, out int t))
	{
		preName = GenerateName(Convert.ToInt32(message), names);
	}
	else
		preName = "Всем";


	Console.WriteLine(@"Введите текст сообщение ('END' -  разорвать соединение, 'GETM' - получить сообщения для себя или всех 'GETC' - получить список активных клиентов)");
	message = Console.ReadLine();

	message += $"\n{preName}";


	// считываем строку в массив байтов
	// при отправке добавляем маркер завершения сообщения - \0
	data = Encoding.UTF8.GetBytes(message + '\0');
	// отправляем данные
	await stream.WriteAsync(data);

	if (message == $"GETM\n{preName}")
	{
		while ((bytesRead = stream.ReadByte()) != '\0')
		{
			response.Add((byte)bytesRead);
		}
		var tempText = Encoding.UTF8.GetString(response.ToArray());
		Console.WriteLine($"Сообщение:\n {tempText}");
		response.Clear();
	}
	else if (message == $"GETC\n{preName}")
	{
		data = Encoding.UTF8.GetBytes(message + '\n');
		await stream.WriteAsync(data);
		names = await GetClientsAsync();
	}

	//// считываем данные до конечного символа
	//while ((bytesRead = stream.ReadByte()) != '\n')
	//{
	//	// добавляем в буфер
	//	response.Add((byte)bytesRead);
	//}
	//var translation = Encoding.UTF8.GetString(response.ToArray());
	//Console.WriteLine($"Слово {message}: {translation}");
	//response.Clear();
	//// имитируем долговременную работу, чтобы одновременно несколько клиентов обрабатывались
	//await Task.Delay(2000);
} while (true);


async Task<List<string>> GetClientsAsync()
{
	var tempList = new List<string>();
	string tempString;
	// буфер для входящих данных
	var tempResponse = new List<byte>();
	// для считывания байтов из потока
	int tempBytesRead = 10;

	//Читаем данные с сервера
	while ((tempBytesRead = stream.ReadByte()) != '\n')
	{
		tempResponse.Add((byte)tempBytesRead);
	}
	tempString = Encoding.UTF8.GetString(tempResponse.ToArray());
	//Расшифровываем и возвращаем
	string[] tempStringArr = tempString.Split(";");
	string resultstr;

	foreach (string str in tempStringArr)
	{
		tempList.Add(str);
	}
	return tempList;
}

string GenerateName(int _number,  List<string> _names)
{
	string[] tempArr = _names.ToArray();
	foreach (string str in tempArr)
	{
		var temp = str.Split(" - ");
		if (Convert.ToInt32(temp[0]) == _number)
		{
			return temp[1];
		}
	}
	return "Всем";
}


