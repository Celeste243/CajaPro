using System;
using MySql.Data.MySqlClient;

namespace CajaPro.Datos
{
    public class Conexion
    {
        private readonly MySqlConnection conexion;

        public Conexion()
        {
            string password = Environment.GetEnvironmentVariable("CAJAPRO_DB_PASSWORD");
            if (string.IsNullOrEmpty(password))
                throw new InvalidOperationException(
                    "Falta la variable de entorno CAJAPRO_DB_PASSWORD.");

            var cs = new MySqlConnectionStringBuilder
            {
                Server = "gateway01.ap-northeast-1.prod.aws.tidbcloud.com",
                Port = 4000,
                Database = "cajapro",
                UserID = "3esHWYVwhCCxXi4.root",
                Password = password,
                SslMode = MySqlSslMode.Required
            };
            conexion = new MySqlConnection(cs.ConnectionString);
        }

        public MySqlConnection AbrirConexion()
        {
            if (conexion.State == System.Data.ConnectionState.Closed)
                conexion.Open();

            return conexion;
        }

        public MySqlConnection CerrarConexion()
        {
            if (conexion.State == System.Data.ConnectionState.Open)
                conexion.Close();

            return conexion;
        }
    }
}