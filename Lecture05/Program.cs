using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Diagnostics;
using System.Xml.Linq;

namespace Lecture05
{
    internal class Program
    {        
        private const string ORIGINAL_FOLDER_PATH = @"C:\test";
        private const string NAS_FOLDER_PATH = @"C:\test1";
        private const string ERROR_FOLDER_PATH = @"C:\test2";
        private const string PYTHON_PATH = @"C:\Python312\python.exe";
        private const string MODEL_PATH = @"C:\pytest\helloworld.py";
        private const string DB_CONNECTION_STRING = @"USER ID=L2TEST;PASSWORD=L2TEST;DATA SOURCE=svc.l2/L2SERVICES;";

        private static OracleConnection _conn = new OracleConnection(DB_CONNECTION_STRING);

        static void Main(string[] args)
        {
            // 초기화 
            if (!InitProgram())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("강제 종료...");
                Environment.Exit(0);
            }

            // 반복 실행
            while (true)
            {
                ModelingAndDbInsertAndFolderMove();
                Thread.Sleep(3000); 
            }
        }

        /// <summary>
        /// 프로그램 초기화
        /// </summary>
        /// <returns>초기화 성공시 true, 실패시 false 반환</returns>
        private static bool InitProgram()
        {
            try
            {
                FileInfo fileInfo = new FileInfo(ORIGINAL_FOLDER_PATH);                
                if (!fileInfo.Exists)
                {
                    DirectoryInfo directory = new DirectoryInfo(ORIGINAL_FOLDER_PATH);
                    directory.Create();
                }

                FileInfo fileInfo2 = new FileInfo(NAS_FOLDER_PATH);
                if (!fileInfo2.Exists)
                {
                    DirectoryInfo directory = new DirectoryInfo(NAS_FOLDER_PATH);
                    directory.Create();
                }

                FileInfo fileInfo3 = new FileInfo(ERROR_FOLDER_PATH);
                if (!fileInfo3.Exists)
                {
                    DirectoryInfo directory = new DirectoryInfo(ERROR_FOLDER_PATH);
                    directory.Create();
                }

                

                if (!CheckDbConnection())
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// DB 연결 확인
        /// </summary>
        /// <returns>연결상태 true/false 반환</returns>
        private static bool CheckDbConnection()
        {
            try
            {
                if (_conn == null)
                {
                    _conn = new OracleConnection(DB_CONNECTION_STRING);
                }

                if (_conn.State == ConnectionState.Closed)
                {
                    _conn.Open();
                }

                return true;
            }
            catch (Exception ex)
            {
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }

            }
        }

        /// <summary>
        /// 이미지파일 이동 및 데이터 베이스 실적 생성
        /// </summary>
        private static void ModelingAndDbInsertAndFolderMove()
        {
            string[] originalFiles = Directory.GetFiles(ORIGINAL_FOLDER_PATH);

            foreach (string originalFile in originalFiles)
            {
                // 하루는 오전 6시에 시작한다.
                DateTime nTime = DateTime.Now.AddHours(-6);
                string y = nTime.ToString("yyyy");
                string m = nTime.ToString("MM");
                string d = nTime.ToString("dd");

                // 01. 경로 정의
                FileInfo originalFileInfo = new FileInfo(originalFile);
                FileInfo moveFileInfo = new FileInfo($@"{NAS_FOLDER_PATH}\{y}\{m}\{d}\{originalFileInfo.Name}");

                // 02. 이동할 폴더 체크                              
                if (!moveFileInfo.Directory.Exists)
                {
                    moveFileInfo.Directory.Create();
                }

                // 03. 파이선 호출
                string pythonResult = CallPython(originalFileInfo.FullName);

                // 04. DB 저장
                if (!DbInsertMes(moveFileInfo, pythonResult))
                {
                    // 05. 저장 실패시 에러폴더로 이동
                    File.Move(originalFileInfo.FullName, ERROR_FOLDER_PATH, true);
                    continue;
                }                

                // 06. NAS 폴더로 파일 이동
                File.Move(originalFileInfo.FullName, moveFileInfo.FullName, true);
            }
        }

        /// <summary>
        /// DB TEST000 데이터 저장
        /// </summary>
        /// <param name="fileInfo">저장할 파일 정보</param>
        /// 
        /// <returns>데이터 정상 저장시 true, 실패시 false</returns>
        private static bool DbInsertMes(FileInfo fileInfo, string pythonResult)
        {
            try 
            {
                string serverPath = fileInfo.FullName.Replace(NAS_FOLDER_PATH, "").Replace(@"\", @"/");
                string url = $"http://localhost{serverPath}";

                // 저장
                string query = $@" INSERT INTO TEST000 (COL1, COL2, COL3, COL4 ) VALUES ( :P_COL1, :P_COL2, :P_COL3, :P_COL4) ";

                OracleCommand cmd = new OracleCommand();
                cmd.Connection = _conn;
                cmd.CommandText = query;
                cmd.Parameters.Clear();
                cmd.Parameters.Add("P_COL1", fileInfo.Name);
                cmd.Parameters.Add("P_COL2", url);
                cmd.Parameters.Add("P_COL3", DateTime.Now);
                cmd.Parameters.Add("P_COL4", pythonResult);
                int rst = cmd.ExecuteNonQuery();
                cmd.Dispose();

                if (rst == 0)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// 파이썬 호출
        /// </summary>
        /// <param name="arg1"></param>
        /// <returns></returns>
        static private string CallPython(string arg)
        {
            var psi = new ProcessStartInfo
            {
                // 파이썬 설치 경로
                FileName = PYTHON_PATH,
                Arguments = $@"{MODEL_PATH} {arg}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var erros = string.Empty;
            var results = string.Empty;

            using (Process process = Process.Start(psi))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    erros = process.StandardError.ReadToEnd();
                    results = process.StandardOutput.ReadToEnd();
                }
            }

            return results;
        }
    }
}