using System;
using System.Data;
using System.Data.SQLite;

namespace database
{
    public class Database
    {
        public SQLiteConnection sqlite_conn;

        public Database()
        {
            // create a new database connection:
            sqlite_conn = new SQLiteConnection("Data Source=database.sqlite;Version=3;");

            // open the connection:
            sqlite_conn.Open();
        }

        public void CloseDB()
        {
            sqlite_conn.Close();
        }

        public void CreateTable()
        {
            SQLiteCommand sqlite_cmd = sqlite_conn.CreateCommand();
            // Let the SQLiteCommand object know our SQL-Query:
            sqlite_cmd.CommandText = "CREATE TABLE settings (id integer primary key, text varchar(100));";
            // Now lets execute the SQL ;-)
            sqlite_cmd.ExecuteNonQuery();

            sqlite_cmd = sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = "INSERT INTO settings (id, text) VALUES (1, '');";
            sqlite_cmd.ExecuteNonQuery();

            sqlite_cmd = sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = "CREATE TABLE mods (id integer NOT NULL UNIQUE, Title varchar(100), Tags varchar(100), ImgURL varchar(100), Description varchar(1000));";
            sqlite_cmd.ExecuteNonQuery();

        }

        public void UpdateWorkshopLocation(string workshopLocation)
        {
            SQLiteCommand sqlite_cmd = sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = "UPDATE settings SET text = '" + workshopLocation + "' where id = 1;";
            sqlite_cmd.ExecuteNonQuery();
        }

        public void InsertNewMod(long id)
        {
            SQLiteCommand sqlite_cmd = sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = "INSERT INTO mods (id,Title,Tags,ImgURL,Description) VALUES (" + id + ", '', '', '', '');";
            sqlite_cmd.ExecuteNonQuery();
        }

        public void UpdateMod(string id, string Title, string Tags, string Description, string Screenshot)
        {
            // escaping
            Description = Description.Replace("'", "''");

            string cmd = String.Format(
                "UPDATE mods SET Title='{0}', Tags='{1}', ImgURL='{2}', Description='{3}' where id={4};",
                Title, Tags, Screenshot, Description, id);

            SQLiteCommand sqlite_cmd = sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = cmd;
            sqlite_cmd.ExecuteNonQuery();
        }

        public string GetWorkshopLocation()
        {
            SQLiteCommand sqlite_cmd;             // Database Command Object
            SQLiteDataReader sqlite_datareader;  // Data Reader Object

            sqlite_cmd = sqlite_conn.CreateCommand();

            sqlite_cmd.CommandText = "SELECT text FROM settings";

            sqlite_datareader = sqlite_cmd.ExecuteReader();

            //// The SQLiteDataReader allows us to run through each row per loop
            //while (sqlite_datareader.Read()) // Read() returns true if there is still a result line to read
            //{
            //    //object idReader = sqlite_datareader.GetValue(0);
            //    //string textReader = sqlite_datareader.GetString(1);
            //}
            string workshopLocation = sqlite_datareader.GetString(1);
            return workshopLocation;
        }

        public void GetMods(DataTable dt)
        {
            const string sqlite_cmd = "SELECT * FROM mods;";
            var da = new SQLiteDataAdapter(sqlite_cmd, sqlite_conn);
            da.Fill(dt);
        }
    }
}