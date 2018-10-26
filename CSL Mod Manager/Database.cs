using System.Data;
using System.Data.SQLite;

namespace CSL_Mod_Manager
{
    public class Database
    {
        public SQLiteConnection sqlite_conn;

        public Database()
        {
            // create a new database connection:
            sqlite_conn = new SQLiteConnection(@"Data Source=database.sqlite;Version=3;");

            // open the connection:
            sqlite_conn.Open();
        }

        public void CloseDB()
        {
            sqlite_conn.Close();
        }

        public void CreateTable()
        {
            var sqlite_cmd = sqlite_conn.CreateCommand();
            // Let the SQLiteCommand object know our SQL-Query:
            sqlite_cmd.CommandText = @"CREATE TABLE settings (id integer primary key, text varchar(100));";
            // Now lets execute the SQL ;-)
            sqlite_cmd.ExecuteNonQuery();

            sqlite_cmd = sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = @"INSERT INTO settings (id, text) VALUES (1, '');";
            sqlite_cmd.ExecuteNonQuery();

            sqlite_cmd = sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = @"CREATE TABLE mods (id integer NOT NULL UNIQUE, Title varchar(100), Size integer, Tags varchar(100), ImgURL varchar(100), Description varchar(1000));";
            sqlite_cmd.ExecuteNonQuery();

        }

        public void UpdateWorkshopLocation(string workshopLocation)
        {
            var sqlite_cmd = sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = $@"UPDATE settings SET text = '{workshopLocation}' where id = 1;";
            sqlite_cmd.ExecuteNonQuery();
        }

        public void InsertNewMod(long id, long size)
        {
            var cmd = $@"INSERT INTO mods (id,Title,Size,Tags,ImgURL,Description) VALUES ({id},'',{size},'','','');";

            var sqlite_cmd = sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = cmd;
            sqlite_cmd.ExecuteNonQuery();
        }

        public void UpdateMod(string id, string Title, string Tags, string Description, string Screenshot)
        {
            // escaping
            Title = Title.Replace("'", "''");
            Description = Description.Replace("'", "''");

            var cmd = $@"UPDATE mods SET Title='{Title}', Tags='{Tags}', ImgURL='{Screenshot}', Description='{Description}' where id={id};";

            var sqlite_cmd = sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = cmd;
            sqlite_cmd.ExecuteNonQuery();
        }

        public void DeleteMod(long id)
        {
            var cmd = $@"DELETE FROM mods where id={id};";

            var sqlite_cmd = sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = cmd;
            sqlite_cmd.ExecuteNonQuery();
        }

        public string GetWorkshopLocation()
        {
            SQLiteCommand sqlite_cmd;             // Database Command Object
            SQLiteDataReader sqlite_datareader;  // Data Reader Object

            sqlite_cmd = sqlite_conn.CreateCommand();

            sqlite_cmd.CommandText = @"SELECT text FROM settings where id=1";

            sqlite_datareader = sqlite_cmd.ExecuteReader();

            var workshopLocation = string.Empty;

            // The SQLiteDataReader allows us to run through each row per loop
            while (sqlite_datareader.Read()) // Read() returns true if there is still a result line to read
            {
                workshopLocation = sqlite_datareader.GetString(sqlite_datareader.GetOrdinal("text"));
            }
            return workshopLocation;
        }

        public void GetMods(DataTable dt)
        {
            const string sqlite_cmd = @"SELECT * FROM mods order by id;";
            var da = new SQLiteDataAdapter(sqlite_cmd, sqlite_conn);
            da.Fill(dt);
        }

        public void GetSpecificMods(DataTable dt, string search)
        {
            search = search.Replace("'", "''");
            var sqlite_cmd = $@"SELECT * FROM mods where Title like '%{search}%' order by id;";
            var da = new SQLiteDataAdapter(sqlite_cmd, sqlite_conn);
            da.Fill(dt);
        }
    }
}