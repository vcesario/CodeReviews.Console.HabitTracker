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
            default:
                break;
        }

    } while (menuOption > 0);

    
    /* Sqlite Snippet for reference
    
    // var command = connection.CreateCommand();
    // command.CommandText =
    // @"
    //     SELECT *
    //     FROM user
    //     WHERE id = $id
    // ";
    // command.Parameters.AddWithValue("$id", 1);

    // using (var reader = command.ExecuteReader())
    // {
    //     while (reader.Read())
    //     {
    //         var name = reader.GetString(0);

    //         Console.WriteLine($"Hello, {name}!");
    //     }
    // }
    */
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
                                    id INT PRIMARY KEY,
                                    habit VARCHAR(20) NOT NULL,
                                    date DATE NOT NULL,
                                    measure FLOAT NOT NULL,
                                    FOREIGN KEY (habit) REFERENCES habitdefs(habit_name)
                                        ON DELETE CASCADE
                                        ON UPDATE CASCADE
                                )";

        command.ExecuteReader();

        Console.WriteLine("Table 'habitlogs' created.");
    }
}

int AskMenuOption()
{
    // menu draft

    // 1. View all entries

    // 2. Log new entry
    // 3. Edit existing entry
    // 4. Remove entry

    // 5. Add new habit definition
    // 6. Edit existing definition
    // 7. Remove definition

    // 8. See statistics

    // 9. [DEBUG] Fill with random entries

    // 0. Exit

    Console.ReadLine();
    return 0;
}