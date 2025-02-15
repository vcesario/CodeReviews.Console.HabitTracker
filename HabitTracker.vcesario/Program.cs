﻿using Microsoft.Data.Sqlite;

using (var connection = new SqliteConnection("Data Source=habittracker.db"))
{
    connection.Open();

    InitializeDBs(connection);

    int menuOption = -1;
    do
    {
        menuOption = AskMenuOption();

        switch (menuOption)
        {
            case 1:
                ViewAllEntries(connection);
                break;
            case 2:
                LogNewEntry(connection);
                break;
            case 3:
                EditEntry(connection);
                break;
            case 4:
                RemoveEntries(connection);
                break;
            case 5:
                AddHabitDefinition(connection);
                break;
            case 6:
                EditDefinition(connection);
                break;
            case 7:
                RemoveDefinition(connection);
                break;
            case 8:
                ViewStatistics(connection);
                break;
            case 9:
                FillDbWithRandomEntries(connection);
                break;
            default:
                break;
        }

    } while (menuOption > 0);
}

void InitializeDBs(SqliteConnection connection)
{
    // check if habitdefs exist. if not, creates it
    try
    {
        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM habitdefs";

        command.ExecuteReader();

        Console.WriteLine("Table 'habitdefs' found.");
    }
    catch (SqliteException) // assuming this will happen only in case table doesn't exist
    {
        var command = connection.CreateCommand();
        command.CommandText = @"CREATE TABLE habitdefs(
                                    habit_name VARCHAR(20) PRIMARY KEY,
                                    unit VARCHAR(10) NOT NULL
                                )";

        command.ExecuteReader();

        Console.WriteLine("Table 'habitdefs' created.");
    }

    try
    {
        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM habitlogs";

        command.ExecuteReader();

        Console.WriteLine("Table 'habitlogs' found.");
    }
    catch (SqliteException)
    {
        var command = connection.CreateCommand();
        command.CommandText = @"CREATE TABLE habitlogs(
                                    id INTEGER PRIMARY KEY,
                                    habit VARCHAR(20) NOT NULL,
                                    date DATE NOT NULL,
                                    measure INT NOT NULL,
                                    FOREIGN KEY (habit) REFERENCES habitdefs(habit_name)
                                        ON DELETE CASCADE
                                        ON UPDATE CASCADE,
                                    UNIQUE (habit, date)
                                )";

        command.ExecuteReader();

        Console.WriteLine("Table 'habitlogs' created.");
    }

    Console.ReadLine();
}

int AskMenuOption()
{
    Console.Clear();
    Console.WriteLine();
    Console.WriteLine("> MAIN MENU");
    Console.WriteLine();
    Console.WriteLine("1. View all entries");
    Console.WriteLine("2. Log new entry");
    Console.WriteLine("3. Edit existing entry");
    Console.WriteLine("4. Remove entry");
    Console.WriteLine("5. Add new habit definition");
    Console.WriteLine("6. Edit existing definition");
    Console.WriteLine("7. Remove definition");
    Console.WriteLine("8. View statistics");
    Console.WriteLine("9. [DEBUG] Fill with random entries");
    Console.WriteLine("0. Exit");
    Console.WriteLine();

    Console.Write("Choose an option: ");
    string? input = Console.ReadLine();
    int option = -1;
    while (input == null || int.TryParse(input, out option) == false || option < 0 || option > 9)
    {
        Console.Write("Invalid option. Try a digit between 0 and 9: ");
        input = Console.ReadLine();
    }

    return option;
}

void ViewAllEntries(SqliteConnection connection)
{
    Console.Clear();
    Console.WriteLine();
    Console.WriteLine("> VIEW ALL ENTRIES");
    Console.WriteLine();

    Console.WriteLine("* Habit definitions");
    Console.WriteLine();

    var command = connection.CreateCommand();
    command.CommandText = "SELECT * FROM habitdefs";
    using (var reader = command.ExecuteReader())
    {
        while (reader.Read())
        {
            var habitName = reader.GetString(0);
            var unit = reader.GetString(1);

            Console.WriteLine($"{habitName} (in {unit})");
        }
    }

    Console.WriteLine();

    Console.WriteLine("* Habit entries");
    Console.WriteLine();

    command.CommandText = @"SELECT habitlogs.habit, habitlogs.date, habitlogs.measure, habitdefs.unit
                            FROM habitlogs
                            INNER JOIN habitdefs ON habitlogs.habit=habitdefs.habit_name
                            ORDER BY habitlogs.date DESC";
    using (var reader = command.ExecuteReader())
    {
        while (reader.Read())
        {
            var habitName = reader.GetString(0);
            var date = DateOnly.FromDateTime(reader.GetDateTime(1));
            var measure = reader.GetInt32(2);
            var unit = reader.GetString(3);

            Console.WriteLine($"{date} {habitName.PadRight(10 + 3)} {measure} {unit}");
        }
    }

    Console.WriteLine();
    Console.ReadLine();
}

void LogNewEntry(SqliteConnection connection)
{
    Console.Clear();
    Console.WriteLine();
    Console.WriteLine("> LOG NEW HABIT ENTRY");
    Console.WriteLine();
    Console.WriteLine("Type in the new entry in the following format:\n"
                        + "\t<yyyy-MM-dd or \"today\"> <habit name> <measure>");
    Console.WriteLine("Type \"return\" to cancel and return to main menu.");

    // get all habit definitions
    Dictionary<string, string> habitToUnit = new();

    var command = connection.CreateCommand();
    command.CommandText = "SELECT * FROM habitdefs";
    using (var reader = command.ExecuteReader())
    {
        while (reader.Read())
        {
            var habitName = reader.GetString(0);
            var unit = reader.GetString(1);

            habitToUnit.Add(habitName, unit);
        }
    }

    if (habitToUnit.Count == 0)
    {
        Console.WriteLine();
        Console.WriteLine("You need to have at least 1 habit defined.");
        Console.ReadLine();
        return;
    }

    Console.WriteLine();
    string? input;
    int successfulInputs = 0;
    DateOnly today = DateOnly.FromDateTime(DateTime.Now);
    DateOnly date = default;
    int measure = 0;
    string habit = string.Empty;

    do
    {
        Console.Write("> ");
        input = Console.ReadLine();

        if (string.IsNullOrEmpty(input))
        {
            continue;
        }

        if (input.ToLower().Equals("return"))
        {
            return;
        }

        string[] inputParts = input.Split();
        if (inputParts.Length != 3)
        {
            Console.WriteLine("Couldn't parse input. Try again.");
            continue;
        }

        successfulInputs = 0;

        if (inputParts[0].Equals("today"))
        {
            date = today;
            successfulInputs++;
        }
        else if (DateOnly.TryParseExact(inputParts[0], "yyyy-MM-dd", out date) && date <= today)
        {
            successfulInputs++;
        }
        else
        {
            Console.WriteLine("Couldn't parse date. Try again.");
        }

        if (habitToUnit.ContainsKey(inputParts[1]))
        {
            habit = inputParts[1];
            successfulInputs++;
        }
        else
        {
            Console.WriteLine("Habit not defined. Try again.");
        }

        if (int.TryParse(inputParts[2], out measure) && measure > 0)
        {
            successfulInputs++;
        }
        else
        {
            Console.WriteLine($"Couldn't parse measure. Try again.");
        }
    }
    while (successfulInputs < 3);

    Console.Clear();
    Console.WriteLine();
    Console.WriteLine("> LOG NEW HABIT ENTRY");
    Console.WriteLine();
    string? inputConfirm;
    bool isSuccessfulInput = false;
    do
    {
        Console.Write($"Confirm \"{measure} {habitToUnit[habit]} of {habit} on {date}\" (y/n): ");
        inputConfirm = Console.ReadLine();

        if (string.IsNullOrEmpty(inputConfirm))
        {
            continue;
        }

        if (inputConfirm.ToLower().Equals("n"))
        {
            return;
        }

        if (inputConfirm.ToLower().Equals("y"))
        {
            isSuccessfulInput = true;
        }
        else
        {
            Console.Write("Couldn't parse input. ");
        }
    }
    while (!isSuccessfulInput);

    Console.WriteLine();
    InsertHabitEntry(habit, date, measure, connection);
    Console.ReadLine();
}

void EditEntry(SqliteConnection connection)
{
    Console.Clear();
    Console.WriteLine();
    Console.WriteLine("> EDIT EXISTING ENTRY");
    Console.WriteLine();
    Console.WriteLine("Type in the existing entry in the following format:\n"
                        + "\t<yyyy-MM-dd> <habit name>");
    Console.WriteLine("Type \"return\" to cancel and return to main menu.");

    Console.WriteLine();
    string? input;
    bool isSuccessfulInput = false;
    DateOnly date = default;
    int measure = 0;
    string habit = string.Empty;
    string unit = string.Empty;

    do
    {
        Console.Write("> ");
        input = Console.ReadLine();

        if (string.IsNullOrEmpty(input))
        {
            continue;
        }

        if (input.ToLower().Equals("return"))
        {
            return;
        }

        string[] inputParts = input.Split();
        if (inputParts.Length != 2)
        {
            Console.WriteLine("Couldn't parse input. Try again.");
            continue;
        }

        if (!DateOnly.TryParseExact(inputParts[0], "yyyy-MM-dd", out date))
        {
            Console.WriteLine("Couldn't parse date. Try again.");
            continue;
        }

        habit = inputParts[1];

        var commandSelect = connection.CreateCommand();
        commandSelect.CommandText = $@"SELECT habitlogs.habit, habitlogs.date, habitlogs.measure, habitdefs.unit FROM habitlogs
                                INNER JOIN habitdefs ON habitlogs.habit=habitdefs.habit_name
                                WHERE habitlogs.habit=@Habit AND habitlogs.date=@Date";

        commandSelect.Parameters.AddWithValue("@Habit", habit);
        commandSelect.Parameters.AddWithValue("@Date", date.ToString("yyyy-MM-dd"));

        using (var reader = commandSelect.ExecuteReader())
        {
            if (!reader.HasRows)
            {
                Console.WriteLine("Couldn't find entry. Try again.");
                continue;
            }

            // i know for a fact that there'd be only one row, because of UNIQUE() in table definition
            reader.Read();

            measure = reader.GetInt32(2);
            unit = reader.GetString(3);
        }

        isSuccessfulInput = true;
    }
    while (!isSuccessfulInput);

    Console.WriteLine($"Found entry: {measure} {unit} of {habit} on {date}.");
    int newMeasure = 0;
    isSuccessfulInput = false;
    do
    {
        Console.Write($"New amount of {unit}: ");
        input = Console.ReadLine();

        if (string.IsNullOrEmpty(input))
        {
            continue;
        }

        if (input.ToLower().Equals("return"))
        {
            return;
        }

        if (!int.TryParse(input, out newMeasure) || newMeasure <= 0)
        {
            Console.Write("Couldn't parse measure. ");
            continue;
        }

        isSuccessfulInput = true;
    }
    while (!isSuccessfulInput);

    var commandUpdate = connection.CreateCommand();
    commandUpdate.CommandText = $@"UPDATE habitlogs
                                    SET measure=@Measure
                                    WHERE habit=@Habit AND date=@Date";
    commandUpdate.Parameters.AddWithValue("@Measure", newMeasure);
    commandUpdate.Parameters.AddWithValue("@Habit", habit);
    commandUpdate.Parameters.AddWithValue("@Date", date.ToString("yyyy-MM-dd"));
    commandUpdate.ExecuteReader();

    Console.WriteLine();
    Console.WriteLine("Habit updated.");
    Console.ReadLine();
}

void RemoveEntries(SqliteConnection connection)
{
    Console.Clear();
    Console.WriteLine();
    Console.WriteLine("> REMOVE ENTRIES");
    Console.WriteLine();

    var command = connection.CreateCommand();
    command.CommandText = @"SELECT * FROM habitlogs
                            ORDER BY date DESC";

    int id = 0;
    string habitName = string.Empty;
    DateOnly date = default;
    int measure = 0;

    using (var reader = command.ExecuteReader())
    {
        while (reader.Read())
        {
            id = reader.GetInt32(0);
            habitName = reader.GetString(1);
            date = DateOnly.FromDateTime(reader.GetDateTime(2));
            measure = reader.GetInt32(3);

            Console.WriteLine($"[#{id}] {date} {habitName.PadRight(10 + 3)} {measure}");
        }
    }

    Console.WriteLine();
    Console.WriteLine("Type in the id (#) of the entry you want to remove.");
    Console.WriteLine("Type \"return\" anytime to cancel and return to main menu.");
    Console.WriteLine();

    string? input;
    bool isSuccessfulInput = false;
    do
    {
        Console.Write("Delete: ");
        input = Console.ReadLine();

        if (string.IsNullOrEmpty(input))
        {
            continue;
        }

        if (input.ToLower().Equals("return"))
        {
            return;
        }

        if (!int.TryParse(input, out id))
        {
            Console.Write("Couldn't parse number. ");
            continue;
        }

        command.CommandText = $@"SELECT * FROM habitlogs
                                WHERE id=@Id";
        command.Parameters.AddWithValue("@Id", id);

        using (var reader = command.ExecuteReader())
        {
            if (!reader.HasRows)
            {
                Console.Write("Entry # not found. ");
                continue;
            }

            reader.Read();

            habitName = reader.GetString(1);
            date = DateOnly.FromDateTime(reader.GetDateTime(2));
            measure = reader.GetInt32(3);
        }

        isSuccessfulInput = true;
    }
    while (!isSuccessfulInput);

    isSuccessfulInput = false;
    do
    {
        Console.Write($"Confirm removal of '[#{id}] {date} {habitName} {measure}' (y/n): ");
        input = Console.ReadLine();

        if (string.IsNullOrEmpty(input))
        {
            continue;
        }
        else if (input.ToLower().Equals("return"))
        {
            return;
        }
        else if (input.ToLower().Equals("n") || input.ToLower().Equals("y"))
        {
            isSuccessfulInput = true;
        }
        else
        {
            Console.Write("Couldn't parse input. ");
        }
    }
    while (!isSuccessfulInput);

    if (!string.IsNullOrEmpty(input) && input.ToLower().Equals("y"))
    {
        command.CommandText = $@"DELETE FROM habitlogs
                                WHERE id=@Id";

        command.ExecuteReader();

        Console.WriteLine();
        Console.WriteLine("Entry deleted.");
        Console.ReadLine();
    }

    RemoveEntries(connection); // ending recursively to print screen again
}

void AddHabitDefinition(SqliteConnection connection)
{
    Console.Clear();
    Console.WriteLine();
    Console.WriteLine("> ADD NEW HABIT DEFINITION");
    Console.WriteLine();

    string? habitName;
    do
    {
        Console.Write("Name the new habit you want to track in this application (max. 20 characters): ");
        habitName = Console.ReadLine();
    } while (habitName == null);

    string? habitUnit;
    do
    {
        Console.Write("What should this habit be measured in (max. 10 characters)? ");
        habitUnit = Console.ReadLine();
    } while (habitUnit == null);

    string? confirm = null;
    do
    {
        Console.Write($"Confirm \"{habitName} (in {habitUnit})\" as a habit (y/n): ");
        confirm = Console.ReadLine();
    } while (confirm == null || (!confirm.ToLower().Equals("y") && !confirm.ToLower().Equals("n")));

    if (confirm.ToLower().Equals("y"))
    {
        InsertHabitDefinition(habitName, habitUnit, connection);
        Console.ReadLine();
    }
    else
    {
        Console.WriteLine();
        Console.Write("Habit definition canceled.");
        Console.ReadLine();
    }
}

void EditDefinition(SqliteConnection connection)
{
    Console.Clear();
    Console.WriteLine();
    Console.WriteLine("> EDIT HABIT DEFINITION");
    Console.WriteLine();
    Console.WriteLine("Type in the current name of the existing habit you wish to edit.");
    Console.WriteLine("Type \"return\" anytime to cancel and return to main menu.");

    Console.WriteLine();
    string? input;
    bool isSuccessfulInput = false;
    string currentHabit = string.Empty;
    string currentUnit = string.Empty;

    do
    {
        Console.Write("Habit name: ");
        input = Console.ReadLine();

        if (string.IsNullOrEmpty(input))
        {
            continue;
        }

        if (input.ToLower().Equals("return"))
        {
            return;
        }

        var commandSelect = connection.CreateCommand();
        commandSelect.CommandText = $@"SELECT * FROM habitdefs
                                        WHERE habit_name=@Input";
        commandSelect.Parameters.AddWithValue("@Input", input);

        using (var reader = commandSelect.ExecuteReader())
        {
            if (!reader.HasRows)
            {
                Console.WriteLine("Couldn't find habit. ");
                continue;
            }

            reader.Read();

            currentHabit = reader.GetString(0);
            currentUnit = reader.GetString(1);
        }

        isSuccessfulInput = true;
    }
    while (!isSuccessfulInput);

    Console.WriteLine();
    Console.WriteLine($"> {currentHabit} {currentUnit}");
    Console.WriteLine();
    string newHabit = string.Empty;
    string newUnit = string.Empty;
    isSuccessfulInput = false;
    do
    {
        Console.Write($"Updated <habit unit>: ");
        input = Console.ReadLine();

        if (string.IsNullOrEmpty(input))
        {
            continue;
        }

        if (input.ToLower().Equals("return"))
        {
            return;
        }

        string[] inputParts = input.Split();
        if (inputParts.Length != 2)
        {
            Console.WriteLine("Couldn't parse input. Try again.");
            continue;
        }

        newHabit = inputParts[0];
        newUnit = inputParts[1];

        isSuccessfulInput = true;
    }
    while (!isSuccessfulInput);

    Console.WriteLine();
    isSuccessfulInput = false;
    do
    {
        Console.Write($"Confirm {currentHabit} ({currentUnit}) -> {newHabit} ({newUnit}) (y/n): ");
        input = Console.ReadLine();

        if (string.IsNullOrEmpty(input))
        {
            continue;
        }

        if (input.ToLower().Equals("n"))
        {
            return;
        }

        if (input.ToLower().Equals("y"))
        {
            isSuccessfulInput = true;
        }
        else
        {
            Console.Write("Couldn't parse input. ");
        }
    }
    while (!isSuccessfulInput);

    var commandUpdate = connection.CreateCommand();
    commandUpdate.CommandText = $@"UPDATE habitdefs
                                    SET habit_name=@NewHabit, unit=@Unit
                                    WHERE habit_name=@CurrentHabit";
    commandUpdate.Parameters.AddWithValue("@NewHabit", newHabit);
    commandUpdate.Parameters.AddWithValue("@Unit", newUnit);
    commandUpdate.Parameters.AddWithValue("@CurrentHabit", currentHabit);

    commandUpdate.ExecuteReader();

    Console.WriteLine();
    Console.WriteLine("Habit definition updated.");
    Console.ReadLine();
}

void RemoveDefinition(SqliteConnection connection)
{
    Console.Clear();
    Console.WriteLine();
    Console.WriteLine("> REMOVE HABIT DEFINITION");
    Console.WriteLine();

    var command = connection.CreateCommand();
    command.CommandText = "SELECT rowid, habit_name, unit FROM habitdefs";

    int id = 0;
    string habitName = string.Empty;
    string unit = string.Empty;

    using (var reader = command.ExecuteReader())
    {
        while (reader.Read())
        {
            id = reader.GetInt32(0);
            habitName = reader.GetString(1);
            unit = reader.GetString(2);

            Console.WriteLine($"[#{id}] {habitName} ({unit})");
        }
    }

    Console.WriteLine();
    Console.WriteLine("Type in the id (#) of the habit you want to remove.");
    Console.WriteLine("[!] Keep in mind that all of the entries associated with this habit will also be removed.");
    Console.WriteLine("Type \"return\" anytime to cancel and return to main menu.");
    Console.WriteLine();

    string? input;
    bool isSuccessfulInput = false;
    do
    {
        Console.Write("Delete: ");
        input = Console.ReadLine();

        if (string.IsNullOrEmpty(input))
        {
            continue;
        }

        if (input.ToLower().Equals("return"))
        {
            return;
        }

        if (!int.TryParse(input, out id))
        {
            Console.Write("Couldn't parse number. ");
            continue;
        }

        command.CommandText = $@"SELECT rowid, habit_name, unit FROM habitdefs
                                WHERE rowid=@Id";
        command.Parameters.AddWithValue("@Id", id);

        using (var reader = command.ExecuteReader())
        {
            if (!reader.HasRows)
            {
                Console.Write("Entry # not found. ");
                continue;
            }

            reader.Read();

            habitName = reader.GetString(1);
            unit = reader.GetString(2);
        }

        isSuccessfulInput = true;
    }
    while (!isSuccessfulInput);

    isSuccessfulInput = false;
    do
    {
        Console.Write($"Confirm removal of '[#{id}] {habitName} ({unit})' (y/n): ");
        input = Console.ReadLine();

        if (string.IsNullOrEmpty(input))
        {
            continue;
        }
        else if (input.ToLower().Equals("return"))
        {
            return;
        }
        else if (input.ToLower().Equals("n") || input.ToLower().Equals("y"))
        {
            isSuccessfulInput = true;
        }
        else
        {
            Console.Write("Couldn't parse input. ");
        }
    }
    while (!isSuccessfulInput);

    if (!string.IsNullOrEmpty(input) && input.ToLower().Equals("y"))
    {
        command.CommandText = $@"DELETE FROM habitdefs
                                WHERE rowid=@Id";

        command.ExecuteReader();

        Console.WriteLine();
        Console.WriteLine("Habit definition deleted.");
        Console.ReadLine();
    }

    RemoveDefinition(connection); // ending recursively to print screen again
}

void ViewStatistics(SqliteConnection connection)
{
    Console.Clear();
    Console.WriteLine();
    Console.WriteLine("> STATISTICS");
    Console.WriteLine();

    Console.WriteLine("Type in the habit you want to see the report of.");
    Console.WriteLine("Type \"return\" anytime to cancel and return to main menu.");
    Console.WriteLine();

    string? input;
    bool isSuccessfulInput = false;
    string habitName = string.Empty;
    string unitName = string.Empty;
    SqliteCommand command;
    do
    {
        Console.Write("Habit name: ");
        input = Console.ReadLine();

        if (string.IsNullOrEmpty(input))
        {
            continue;
        }

        if (input.ToLower().Equals("return"))
        {
            return;
        }

        command = connection.CreateCommand();
        command.CommandText = $@"SELECT * FROM habitdefs
                                    WHERE habit_name=@Input";
        command.Parameters.AddWithValue("@Input", input);

        using (var reader = command.ExecuteReader())
        {
            if (!reader.HasRows)
            {
                Console.Write("Couldn't find habit. ");
                continue;
            }

            reader.Read();

            habitName = reader.GetString(0);
            unitName = reader.GetString(1);
        }

        isSuccessfulInput = true;
    }
    while (!isSuccessfulInput);

    Console.Clear();
    Console.WriteLine();
    Console.WriteLine("> STATISTICS");
    Console.WriteLine();

    // top 3 days
    command = connection.CreateCommand();
    command.CommandText = $@"SELECT * FROM habitlogs
                                WHERE habit=@HabitName
                                ORDER BY measure DESC
                                LIMIT 3";
    command.Parameters.AddWithValue("@HabitName", habitName);

    using (var reader = command.ExecuteReader())
    {
        if (!reader.HasRows)
        {
            Console.WriteLine("No entries for this habit.");
            Console.ReadLine();
            return;
        }
        else
        {
            Console.WriteLine($"* Your top 3 days of {habitName} were:");

            while (reader.Read())
            {
                var date = DateOnly.FromDateTime(reader.GetDateTime(2));
                var measure = reader.GetInt32(3);
                Console.WriteLine($"\t[{date}] {measure} {unitName}");
            }
        }
    }

    Console.ReadLine();

    // Top 3 week and month
    command = connection.CreateCommand();
    command.CommandText = $@"SELECT * FROM habitlogs
                                WHERE habit=@HabitName
                                ORDER BY date DESC";
    command.Parameters.AddWithValue("@HabitName", habitName);

    using (var reader = command.ExecuteReader())
    {
        // instantiate struct that contains earliest date of this week, and sum
        List<WeekSum> weekSums = new();
        WeekSum weekSum = new(DateOnly.FromDateTime(DateTime.Today));
        List<MonthSum> monthSums = new();
        MonthSum monthSum = new(DateOnly.FromDateTime(DateTime.Today));

        DateOnly previousRowDate = DateOnly.FromDateTime(DateTime.Today);
        int ongoingStreak = 0;
        int maxStreak = 0;
        int streak = 0;
        bool isOngoingStreak = true;

        while (reader.Read())
        {
            // if date of current row exits structs' range, decrease struct's range until it fits
            // add current row's measure to sum

            DateOnly rowDate = DateOnly.FromDateTime(reader.GetDateTime(2));

            if ((previousRowDate.DayNumber - rowDate.DayNumber) <= 1)
            {
                streak++;
            }
            else
            {
                if (isOngoingStreak)
                {
                    isOngoingStreak = false;
                    ongoingStreak = streak;
                }

                maxStreak = streak > maxStreak ? streak : maxStreak;
                streak = 1;
            }

            if (rowDate < weekSum.Start)
            {
                weekSums.Add(weekSum);
                while (rowDate < weekSum.Start)
                {
                    weekSum.Start = weekSum.Start.AddDays(-7);
                }
                weekSum.Sum = 0;
            }

            if (rowDate < monthSum.Start)
            {
                monthSums.Add(monthSum);
                monthSum = new(rowDate);
            }

            int rowMeasure = reader.GetInt32(3);
            weekSum.Sum += rowMeasure;
            monthSum.Sum += rowMeasure;

            previousRowDate = rowDate;
        }

        weekSums.Add(weekSum);
        monthSums.Add(monthSum);

        weekSums.Sort((_sum1, _sum2) => _sum2.Sum.CompareTo(_sum1.Sum));
        monthSums.Sort((_sum1, _sum2) => _sum2.Sum.CompareTo(_sum1.Sum));

        Console.WriteLine($"* Your top 3 weeks of {habitName} were:");
        int i = 0;
        while (i < 3 && i < weekSums.Count)
        {
            Console.WriteLine($"\t[{weekSums[i].Start} - {weekSums[i].End}] {weekSums[i].Sum} {unitName}");
            i++;
        }
        Console.ReadLine();

        Console.WriteLine($"* Your top 3 months of {habitName} were:");
        i = 0;
        while (i < 3 && i < monthSums.Count)
        {
            Console.WriteLine($"\t[{monthSums[i].Start.ToString("MMM")} {monthSums[i].Start.Year}] {monthSums[i].Sum} {unitName}");
            i++;
        }
        Console.ReadLine();

        Console.Write($@"  You currently hold a streak of {ongoingStreak} days,");
        Console.ReadLine();
        Console.WriteLine($@"  and your best streak totalized {maxStreak} days. Keep going!");
        Console.ReadLine();
    }

    // C.T.A. (count, total, avg)
    command = connection.CreateCommand();
    command.CommandText = $@"SELECT COUNT(*) FROM habitlogs
                                WHERE habit=@HabitName";
    command.Parameters.AddWithValue("@HabitName", habitName);
    using (var reader = command.ExecuteReader())
    {
        reader.Read();

        Console.Write($@"  You performed {reader.GetInt32(0)} days of {habitName},");
        Console.ReadLine();
    }

    command = connection.CreateCommand();
    command.CommandText = $@"SELECT SUM(measure) FROM habitlogs
                                WHERE habit=@HabitName";
    command.Parameters.AddWithValue("@HabitName", habitName);
    using (var reader = command.ExecuteReader())
    {
        reader.Read();

        Console.Write($@"  totalizing {reader.GetInt32(0)} {unitName}...");
        Console.ReadLine();
    }

    command = connection.CreateCommand();
    command.CommandText = $@"SELECT AVG(measure) FROM habitlogs
                                WHERE habit=@HabitName";
    command.Parameters.AddWithValue("@HabitName", habitName);
    using (var reader = command.ExecuteReader())
    {
        reader.Read();

        Console.WriteLine($@"  ...with an average of {reader.GetFloat(0)} {unitName} per day.");
    }
    Console.WriteLine();
    Console.WriteLine("\t-- END OF REPORT --");
    Console.ReadLine();
}

void FillDbWithRandomEntries(SqliteConnection connection)
{
    Console.Clear();

    // define 3 habits
    //  cycling in kilometers
    //  walking in steps
    //  water in glasses
    //
    // loop 1 to a 100, date d in reversed order starting from today
    // for each of the above habits
    //  50% chance of adding an entry with date d and random measure value

    InsertHabitDefinition("cycling", "kilometers", connection);
    InsertHabitDefinition("walking", "steps", connection);
    InsertHabitDefinition("water", "glasses", connection);

    Dictionary<string, int[]> measureRangesPerHabit = new()
    {
        { "cycling", new int[2] { 5, 50 } },
        { "walking", new int[2] { 500, 3000 } },
        { "water",   new int[2] { 1, 10 } },
    };

    DateOnly today = DateOnly.FromDateTime(DateTime.Today);
    Random random = new Random();

    for (int i = 0; i < 100; i++)
    {
        DateOnly date = today.AddDays(-i);

        if (random.NextSingle() >= 0.5f) // cycling
        {
            int randomMin = measureRangesPerHabit["cycling"][0];
            int randomMax = measureRangesPerHabit["cycling"][1];
            int randomMeasure = random.Next(randomMin, randomMax + 1);

            InsertHabitEntry("cycling", date, randomMeasure, connection);
        }

        if (random.NextSingle() >= 0.5f) // walking
        {
            int randomMin = measureRangesPerHabit["walking"][0];
            int randomMax = measureRangesPerHabit["walking"][1];
            int randomMeasure = random.Next(randomMin, randomMax + 1);

            InsertHabitEntry("walking", date, randomMeasure, connection);
        }

        if (random.NextSingle() >= 0.5f) // water
        {
            int randomMin = measureRangesPerHabit["water"][0];
            int randomMax = measureRangesPerHabit["water"][1];
            int randomMeasure = random.Next(randomMin, randomMax + 1);

            InsertHabitEntry("water", date, randomMeasure, connection);
        }
    }

    Console.ReadLine();
}

void InsertHabitDefinition(string habitName, string unit, SqliteConnection connection)
{
    try
    {
        var command = connection.CreateCommand();
        command.CommandText = $@"INSERT INTO habitdefs (habit_name, unit)
                                    VALUES (@HabitName, @Unit)";
        command.Parameters.AddWithValue("@HabitName", habitName);
        command.Parameters.AddWithValue("@Unit", unit);

        command.ExecuteReader();

        Console.WriteLine($"'{habitName} ({unit})' habit created.");
    }
    catch (SqliteException)
    {
        // habit already defined
        Console.WriteLine($"'{habitName} ({unit})' habit already defined.");
    }
}

void InsertHabitEntry(string habit, DateOnly date, int measure, SqliteConnection connection)
{
    try
    {
        var command = connection.CreateCommand();
        command.CommandText = $@"INSERT INTO habitlogs (habit, date, measure)
                                            VALUES (@HabitName, @Date, @Measure)";
        command.Parameters.AddWithValue("@HabitName", habit);
        command.Parameters.AddWithValue("@Date", date.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("@Measure", measure);
        command.ExecuteReader();

        Console.WriteLine($"Added entry ['{habit}', '{date.ToString("yyyy-MM-dd")}', '{measure}']");
    }
    catch (SqliteException)
    {
        // entry already defined for this habit and date
        Console.WriteLine($"Entry ['{habit}', '{date}'] already defined.");
    }
}

struct WeekSum
{
    public DateOnly Start;                      // earliest sunday
    public DateOnly End => Start.AddDays(6);    // latest saturday
    public int Sum;

    public WeekSum(DateOnly reference)
    {
        Start = reference.AddDays(-(int)reference.DayOfWeek);
        Sum = 0;
    }
}

struct MonthSum
{
    public DateOnly Start;
    public int Sum;

    public MonthSum(DateOnly reference)
    {
        Start = new DateOnly(reference.Year, reference.Month, 1);
        Sum = 0;
    }
}