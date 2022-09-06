using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Reflection;
using System.IO;

namespace LogAnalysisTools
{
    public enum SQLiteDb
    {
        Null, sqlite_db
    }

    public class SQLite
    {

        private SQLiteConnection Conn = null;
        private SQLiteCommand Comm = null;
        private SQLiteTransaction Tran = null;
        private bool TransactionIsOpen = false;
        private string ConnString = null;
        private int CommTimeOut = 60;
        private string root = null;
        public string SQL = null;
        private StringBuilder SQLBuilder = null;

        private static Dictionary<SQLiteDb, string> _ConnString = new Dictionary<SQLiteDb, string>();

        private SQLite()
        {
        }

        private SQLite(SQLiteDb db)
        {
            if (db == SQLiteDb.Null)
            {
                Exception e = new Exception("SQLite database not found!");
                throw e;
            }

            if (_ConnString.ContainsKey(db))
                this.ConnString = _ConnString[db];
            else
            {
                this.ConnString = string.Format("{0}{1}.db", Root, Enum.GetName(typeof(SQLiteDb), db));
                this.ConnString = string.Format("Data Source={0};Version=3;Pooling=true;FailIfMissing=true", this.ConnString);
                _ConnString[db] = this.ConnString;
            }
        }

        public string Root
        {
            get
            {
                if (root == null)
                {
                    string f = Assembly.GetExecutingAssembly().CodeBase.Substring(8);
                    f = Path.GetDirectoryName(f);
                    f = f.Substring(0, f.LastIndexOf(Path.DirectorySeparatorChar));
                    root = string.Format("{0}{1}", f, Path.DirectorySeparatorChar);
                }
                return root;
            }
        }


        public static SQLite Instance(SQLiteDb db)
        {
            return new SQLite(db);
        }

        public static string Date
        {
            get { return DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff", System.Globalization.DateTimeFormatInfo.InvariantInfo); }
        }

        public static string Date2
        {
            get { return DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.DateTimeFormatInfo.InvariantInfo); }
        }

        public void TimeOut(int timeOut)
        {
            this.CommTimeOut = timeOut;
            if (this.Comm != null)
                this.Comm.CommandTimeout = this.CommTimeOut;
        }

        public string Escape(string data)
        {
            if (data == null)
                return "";
            data = data.Replace("'", "''");
            return data;
        }


        public SQLite Builder()
        {
            this.SQLBuilder = new StringBuilder();
            return this;
        }

        public SQLite Builder(object data)
        {
            this.SQLBuilder.Append(data);
            return this;
        }

        public SQLite Builder(string data, object value)
        {
            this.SQLBuilder.Append(string.Format(data, value));
            return this;
        }

        public SQLite Builder(bool isParams, params object[] data)
        {
            foreach (object o in data)
            {
                this.SQLBuilder.Append(o);
            }
            return this;
        }

        public void Builder(bool Completed)
        {
            this.SQL = this.SQLBuilder.ToString();
            this.SQLBuilder = null;
        }


        public void Connection()
        {
            if (this.Conn == null || this.Conn.State != ConnectionState.Open)
            {
                try
                {
                    this.Conn = new SQLiteConnection(this.ConnString);
                    this.Conn.Open();
                }
                catch (Exception e)
                {
                    throw e;
                }
            }

            if (this.Comm == null)
            {
                this.Comm = new SQLiteCommand();
            }
            this.Comm.Connection = this.Conn;
            this.Comm.CommandTimeout = this.CommTimeOut;
        }

        public void DisConnection()
        {
            if (this.Comm != null)
            {
                this.Comm.Dispose();
                this.Comm = null;
            }

            if (this.Conn != null)
            {
                try
                {
                    if (this.Conn.State != ConnectionState.Closed)
                    {
                        this.Conn.Close();
                    }
                }
                catch { }
                this.Conn.Dispose();
                this.Conn = null;
            }
        }


        public void Transaction()
        {
            this.Connection();
            this.Tran = this.Conn.BeginTransaction();
            this.TransactionIsOpen = true;
        }

        public void Commit()
        {
            this.Tran.Commit();
            this.TransactionIsOpen = false;
            this.DisConnection();
        }

        public void Rollback()
        {
            this.Tran.Rollback();
            this.TransactionIsOpen = false;
            this.DisConnection();
        }


        private string doBuildCommandText(IEnumerable<SQLiteParameter> parameters)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(this.Comm.CommandText);
            if (parameters != null)
            {
                sb.Append("\r\n");
                foreach (var parameter in parameters)
                    sb.Append(string.Format("{0} = {1};\r\n", parameter.ParameterName, parameter.Value));
            }
            return sb.ToString();
        }


        private List<SQLiteParameter> ParameterList(Dictionary<string, object> parameters)
        {
            List<SQLiteParameter> lst = new List<SQLiteParameter>();
            if (parameters != null)
            {
                foreach (KeyValuePair<string, object> kv in parameters)
                {
                    lst.Add(new SQLiteParameter(kv.Key, kv.Value));
                }
            }
            return lst;
        }

        public int Execute(string SQL, IEnumerable<SQLiteParameter> parameters = null)
        {
            this.Connection();
            this.Comm.CommandTimeout = this.CommTimeOut;
            this.Comm.Connection = this.Conn;
            if (this.TransactionIsOpen)
                this.Comm.Transaction = this.Tran;
            this.Comm.CommandType = CommandType.Text;
            this.Comm.CommandText = SQL;
            this.Comm.Parameters.Clear();
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    this.Comm.Parameters.Add(parameter);
                }
            }

            try
            {
                int Ret = 0;
                //using (SQLiteWriteLock sqliteLock = new SQLiteWriteLock(this.CurrSQLiteDb))
                {
                    Ret = this.Comm.ExecuteNonQuery();
                }
                return Ret;
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (!this.TransactionIsOpen)
                {
                    this.DisConnection();
                }
            }
        }

        public int Execute(string SQL, Dictionary<string, object> parameters = null)
        {
            List<SQLiteParameter> lst = ParameterList(parameters);
            return this.Execute(SQL, lst);
        }

        public int Execute(string SQL, params object[] parameters)
        {
            Dictionary<string, object> lst = new Dictionary<string, object>();
            for (int i = 0; i < parameters.Length; i = i + 2)
            {
                lst[parameters[i].ToString()] = parameters[i + 1];
            }
            return this.Execute(SQL, lst);
        }

        public int Execute(IEnumerable<SQLiteParameter> parameters = null)
        {
            return this.Execute(this.SQL, parameters);
        }

        public int Execute(Dictionary<string, object> parameters = null)
        {
            return this.Execute(this.SQL, parameters);
        }

        public int Execute(bool isParams, params object[] parameters)
        {
            return this.Execute(this.SQL, parameters);
        }

        public int Execute(string SQL)
        {
            IEnumerable<SQLiteParameter> lst = null;
            return this.Execute(SQL, lst);
        }

        public int Execute()
        {
            IEnumerable<SQLiteParameter> lst = null;
            return this.Execute(this.SQL, lst);
        }


        public T Scalar<T>(string SQL, IEnumerable<SQLiteParameter> parameters = null)
        {
            this.Connection();
            this.Comm.CommandTimeout = this.CommTimeOut;
            this.Comm.Connection = this.Conn;
            if (this.TransactionIsOpen)
                this.Comm.Transaction = this.Tran;
            this.Comm.CommandType = CommandType.Text;
            this.Comm.CommandText = SQL;
            this.Comm.Parameters.Clear();
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    this.Comm.Parameters.Add(parameter);
                }
            }

            try
            {
                var ret = this.Comm.ExecuteScalar();
                if (ret != null && ret != DBNull.Value)
                    return (T)Convert.ChangeType(ret, typeof(T));
                else
                    return default(T);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (!this.TransactionIsOpen)
                {
                    this.DisConnection();
                }
            }
        }

        public T Scalar<T>(string SQL, Dictionary<string, object> parameters = null)
        {
            List<SQLiteParameter> lst = ParameterList(parameters);
            return this.Scalar<T>(SQL, lst);
        }

        public T Scalar<T>(string SQL, params object[] parameters)
        {
            Dictionary<string, object> lst = new Dictionary<string, object>();
            for (int i = 0; i < parameters.Length; i = i + 2)
            {
                lst[parameters[i].ToString()] = parameters[i + 1];
            }
            return this.Scalar<T>(SQL, lst);
        }

        public T Scalar<T>(IEnumerable<SQLiteParameter> parameters = null)
        {
            return this.Scalar<T>(this.SQL, parameters);
        }

        public T Scalar<T>(Dictionary<string, object> parameters = null)
        {
            return this.Scalar<T>(this.SQL, parameters);
        }

        public T Scalar<T>(bool isParams, params object[] parameters)
        {
            return this.Scalar<T>(this.SQL, parameters);
        }

        public T Scalar<T>(string SQL)
        {
            IEnumerable<SQLiteParameter> lst = null;
            return this.Scalar<T>(SQL, lst);
        }

        public T Scalar<T>()
        {
            IEnumerable<SQLiteParameter> lst = null;
            return this.Scalar<T>(this.SQL, lst);
        }


        public List<Dictionary<string, object>> ListDictionary(string SQL, IEnumerable<SQLiteParameter> parameters = null)
        {
            this.Connection();
            this.Comm.CommandTimeout = this.CommTimeOut;
            this.Comm.Connection = this.Conn;
            if (this.TransactionIsOpen)
                this.Comm.Transaction = this.Tran;
            this.Comm.CommandType = CommandType.Text;
            this.Comm.CommandText = SQL;
            this.Comm.Parameters.Clear();
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    this.Comm.Parameters.Add(parameter);
                }
            }

            try
            {
                using (SQLiteDataReader reader = this.Comm.ExecuteReader())
                {
                    var list = new List<Dictionary<string, object>>();
                    if (reader != null && !reader.IsClosed && reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            var dic = new Dictionary<string, object>();
                            int i = reader.FieldCount;
                            for (int j = 0; j < i; j++)
                            {
                                dic.Add(reader.GetName(j).ToUpper(), reader.GetValue(j));
                            }
                            list.Add(dic);
                        }
                        reader.Close();
                    }
                    return list;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (!this.TransactionIsOpen)
                {
                    this.DisConnection();
                }
            }
        }

        public List<Dictionary<string, object>> ListDictionary(string SQL, Dictionary<string, object> parameters = null)
        {
            List<SQLiteParameter> lst = ParameterList(parameters);
            return this.ListDictionary(SQL, lst);
        }

        public List<Dictionary<string, object>> ListDictionary(string SQL, params object[] parameters)
        {
            Dictionary<string, object> lst = new Dictionary<string, object>();
            for (int i = 0; i < parameters.Length; i = i + 2)
            {
                lst[parameters[i].ToString()] = parameters[i + 1];
            }
            return this.ListDictionary(SQL, lst);
        }

        public List<Dictionary<string, object>> ListDictionary(IEnumerable<SQLiteParameter> parameters = null)
        {
            return this.ListDictionary(this.SQL, parameters);
        }

        public List<Dictionary<string, object>> ListDictionary(Dictionary<string, object> parameters = null)
        {
            return this.ListDictionary(this.SQL, parameters);
        }

        public List<Dictionary<string, object>> ListDictionary(bool isParams, params object[] parameters)
        {
            return this.ListDictionary(this.SQL, parameters);
        }

        public List<Dictionary<string, object>> ListDictionary(string SQL)
        {
            IEnumerable<SQLiteParameter> lst = null;
            return this.ListDictionary(SQL, lst);
        }

        public List<Dictionary<string, object>> ListDictionary()
        {
            IEnumerable<SQLiteParameter> lst = null;
            return this.ListDictionary(this.SQL, lst);
        }


        public DataTable Select(string SQL, IEnumerable<SQLiteParameter> parameters = null)
        {
            this.Connection();
            this.Comm.CommandTimeout = this.CommTimeOut;
            this.Comm.Connection = this.Conn;
            if (this.TransactionIsOpen)
                this.Comm.Transaction = this.Tran;
            this.Comm.CommandType = CommandType.Text;
            this.Comm.CommandText = SQL;
            this.Comm.Parameters.Clear();
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    this.Comm.Parameters.Add(parameter);
                }
            }

            try
            {
                using (SQLiteDataAdapter Da = new SQLiteDataAdapter(this.Comm))
                {
                    DataSet Ds = new DataSet();
                    Da.Fill(Ds);
                    return Ds.Tables[0];
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (!this.TransactionIsOpen)
                {
                    this.DisConnection();
                }
            }
        }

        public DataTable Select(string SQL, Dictionary<string, object> parameters = null)
        {
            List<SQLiteParameter> lst = ParameterList(parameters);
            return this.Select(SQL, lst);
        }

        public DataTable Select(string SQL, params object[] parameters)
        {
            Dictionary<string, object> lst = new Dictionary<string, object>();
            for (int i = 0; i < parameters.Length; i = i + 2)
            {
                lst[parameters[i].ToString()] = parameters[i + 1];
            }
            return this.Select(SQL, lst);
        }

        public DataTable Select(IEnumerable<SQLiteParameter> parameters = null)
        {
            return this.Select(this.SQL, parameters);
        }

        public DataTable Select(Dictionary<string, object> parameters = null)
        {
            return this.Select(this.SQL, parameters);
        }

        public DataTable Select(bool isParams, params object[] parameters)
        {
            return this.Select(this.SQL, parameters);
        }

        public DataTable Select(string SQL)
        {
            IEnumerable<SQLiteParameter> lst = null;
            return this.Select(SQL, lst);
        }

        public DataTable Select()
        {
            IEnumerable<SQLiteParameter> lst = null;
            return this.Select(this.SQL, lst);
        }


        public int Insert(string Table, Dictionary<string, object> Column)
        {
            StringBuilder sbCol = new System.Text.StringBuilder();
            StringBuilder sbVal = new System.Text.StringBuilder();
            foreach (KeyValuePair<string, object> kv in Column)
            {
                if (sbCol.Length == 0)
                {
                    sbCol.Append("insert into ");
                    sbCol.Append(Table);
                    sbCol.Append("(");
                }
                else
                {
                    sbCol.Append(",");
                }
                sbCol.Append("`");
                sbCol.Append(kv.Key);
                sbCol.Append("`");

                if (sbVal.Length == 0)
                {
                    sbVal.Append(" values (");
                }
                else
                {
                    sbVal.Append(",");
                }
                sbVal.Append("@v");
                sbVal.Append(kv.Key);
            }
            sbCol.Append(") ");
            sbVal.Append(");");

            this.SQL = sbCol.ToString() + sbVal.ToString();
            Dictionary<string, object> lst = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> kv in Column)
            {
                lst.Add("@v" + kv.Key, kv.Value);
            }
            return this.Execute(lst);
        }

        public int Insert(Dictionary<string, object> Column)
        {
            Dictionary<string, object> lst = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> kv in Column)
            {
                lst.Add("@" + kv.Key, kv.Value);
            }
            return this.Execute(lst);
        }

        public int InsertRowId()
        {
            return this.Scalar<int>("select last_insert_rowid();");
        }


        public int Update(string Table, Dictionary<string, object> Data, Dictionary<string, object> Where)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("update `");
            sb.Append(Table);
            sb.Append("` set ");

            bool firstRecord = true;
            foreach (KeyValuePair<string, object> kv in Data)
            {
                if (firstRecord)
                    firstRecord = false;
                else
                    sb.Append(",");
                sb.Append("`");
                sb.Append(kv.Key);
                sb.Append("` = ");
                sb.Append("@v");
                sb.Append(kv.Key);
            }

            if (Where != null && Where.Count > 0)
            {
                sb.Append(" where ");
                firstRecord = true;
                foreach (KeyValuePair<string, object> kv in Where)
                {
                    if (firstRecord)
                        firstRecord = false;
                    else
                        sb.Append(" and ");
                    sb.Append("`");
                    sb.Append(kv.Key);
                    sb.Append("` = ");
                    sb.Append("@c");
                    sb.Append(kv.Key);
                }
            }
            sb.Append(";");

            this.SQL = sb.ToString();
            Dictionary<string, object> lst = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> kv in Data)
            {
                lst.Add("@v" + kv.Key, kv.Value);
            }
            foreach (KeyValuePair<string, object> kv in Where)
            {
                lst.Add("@c" + kv.Key, kv.Value);
            }
            return this.Execute(lst);
        }

        public int Update(string Table, Dictionary<string, object> Data, string Column, object Value)
        {
            Dictionary<string, object> Where = new Dictionary<string, object>();
            Where[Column] = Value;
            return Update(Table, Data, Where);
        }

        public int Update(string Table, Dictionary<string, object> Data, params object[] Values)
        {
            Dictionary<string, object> Where = new Dictionary<string, object>();
            for (int i = 0; i < Values.Length; i = i + 2)
            {
                Where[Values[i].ToString()] = Values[i + 1];
            }
            return Update(Table, Data, Where);
        }


        public int Delete(string Table, Dictionary<string, object> Where)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("delete from `");
            sb.Append(Table);
            sb.Append("` ");

            if (Where != null && Where.Count > 0)
            {
                sb.Append(" where ");
                bool firstRecord = true;
                foreach (KeyValuePair<string, object> kv in Where)
                {
                    if (firstRecord)
                        firstRecord = false;
                    else
                        sb.Append(" and ");
                    sb.Append("`");
                    sb.Append(kv.Key);
                    sb.Append("` = ");
                    sb.Append("@c");
                    sb.Append(kv.Key);
                }
            }
            sb.Append(";");

            this.SQL = sb.ToString();
            Dictionary<string, object> lst = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> kv in Where)
            {
                lst.Add("@c" + kv.Key, kv.Value);
            }
            return this.Execute(lst);
        }

        public int Delete(string Table, string Column, object Value)
        {
            Dictionary<string, object> Where = new Dictionary<string, object>();
            Where[Column] = Value;
            return Delete(Table, Where);
        }

        public int Delete(string Table, params object[] Values)
        {
            Dictionary<string, object> Where = new Dictionary<string, object>();
            for (int i = 0; i < Values.Length; i = i + 2)
            {
                Where[Values[i].ToString()] = Values[i + 1];
            }
            return Delete(Table, Where);
        }


        public bool Exists(string Table, Dictionary<string, object> Where)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("select count(1) as num from ");
            sb.Append(Table);

            if (Where != null && Where.Count > 0)
            {
                sb.Append(" where ");
                bool firstRecord = true;
                foreach (KeyValuePair<string, object> kv in Where)
                {
                    if (firstRecord)
                        firstRecord = false;
                    else
                        sb.Append(" and ");
                    sb.Append("`");
                    sb.Append(kv.Key);
                    sb.Append("` = ");
                    sb.Append("@c");
                    sb.Append(kv.Key);
                }
            }
            sb.Append(" limit 1;");

            this.SQL = sb.ToString();
            Dictionary<string, object> lst = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> kv in Where)
            {
                lst.Add("@c" + kv.Key, kv.Value);
            }
            return this.Scalar<int>(lst) > 0;
        }

        public bool Exists(string Table, string Column, object Value)
        {
            Dictionary<string, object> Where = new Dictionary<string, object>();
            Where[Column] = Value;
            return Exists(Table, Where);
        }

        public bool Exists(string Table, params object[] Values)
        {
            Dictionary<string, object> Where = new Dictionary<string, object>();
            for (int i = 0; i < Values.Length; i = i + 2)
            {
                Where[Values[i].ToString()] = Values[i + 1];
            }
            return Exists(Table, Where);
        }
    }
}
