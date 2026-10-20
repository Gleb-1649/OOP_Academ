using Kiosk;

var machine = new VendingService();
machine.AddProduct(new CatalogEntry("tea", "Холодный чай", 17, 4));
machine.AddProduct(new CatalogEntry("juice", "Сок", 24, 3));
machine.AddProduct(new CatalogEntry("bar", "Злаковый батончик", 12, 5));

Console.WriteLine("Автомат: list, coin, buy, cancel, admin, exit.");
bool running = true;
while (running)
{
    Console.Write($"Баланс {machine.Balance} руб. Команда: ");
    string? command = Console.ReadLine();
    if (command is null) break;
    try
    {
        switch (command.Trim().ToLowerInvariant())
        {
            case "list":
                foreach (var p in machine.Products)
                    Console.WriteLine($"{p.Code}: {p.Name}, {p.Price} руб., остаток {p.Stock}");
                break;
            case "coin":
                machine.InsertCoin(ReadNumber("Номинал (1, 2, 5, 10): "));
                break;
            case "buy":
                Purchase purchase = machine.Buy(ReadText("Код товара: "));
                Console.WriteLine($"Выдано: {purchase.ProductName}. Сдача: {purchase.Change} руб.");
                Console.WriteLine("Монеты сдачи: " + string.Join(", ", purchase.ChangeCoins));
                break;
            case "cancel":
                Console.WriteLine("Возвращены монеты: " + string.Join(", ", machine.Cancel()));
                break;
            case "admin":
                AdminMenu(machine);
                break;
            case "exit":
                Console.WriteLine("Возвращены монеты: " + string.Join(", ", machine.Cancel()));
                running = false;
                break;
            default:
                Console.WriteLine("Команды: list, coin, buy, cancel, admin, exit.");
                break;
        }
    }
    catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException or OverflowException)
    {
        Console.WriteLine("Ошибка: " + ex.Message);
    }
}

static void AdminMenu(VendingService machine)
{
    string command = ReadText("Администратор: add (новый товар), stock (пополнить), collect (касса): ");
    switch (command.Trim().ToLowerInvariant())
    {
        case "add":
            string code = ReadText("Код: ");
            string name = ReadText("Название: ");
            int price = ReadNumber("Цена в рублях: ");
            int stock = ReadNumber("Количество: ");
            machine.AddProduct(new CatalogEntry(code, name, price, stock));
            Console.WriteLine("Товар добавлен.");
            break;
        case "stock":
            machine.Restock(ReadText("Код: "), ReadNumber("Добавить единиц: "));
            Console.WriteLine("Запас пополнен.");
            break;
        case "collect":
            Console.WriteLine($"Из кассы собрано {machine.CollectCash()} руб.");
            break;
        default:
            Console.WriteLine("Неизвестная команда администратора.");
            break;
    }
}

static string ReadText(string prompt)
{
    Console.Write(prompt);
    return Console.ReadLine() ?? throw new InvalidOperationException("Ввод завершен.");
}

static int ReadNumber(string prompt)
{
    if (!int.TryParse(ReadText(prompt), out int number))
        throw new ArgumentException("Введите целое число.");
    return number;
}
