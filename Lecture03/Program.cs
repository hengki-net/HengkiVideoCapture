using OpcUa;
using OpenCvSharp;

namespace Lecture03
{
    internal class Program
    {
        public static bool save = false;

        static void Main()
        {
            // OPC 설정            
            string[] tagList = { "test.N01.I001", };
            OpcUaClient opcClient = new OpcUaClient();
            opcClient.getData += (string tag, string value) =>
            {
                Console.WriteLine($@"{tag} {value}");
                if(tag.Equals("test.N01.I001") && value.Equals("20"))
                {
                    save = true;
                }
            };
            Task task = opcClient.StartOpcua("opc.tcp://172.31.5.10:49320", tagList); 


            // 이미지 처리
            imgProc();
        }


        private static void imgProc()
        {
            // RTSP 영상 가져오기
            VideoCapture capture = new VideoCapture();
            capture.Open("rtsp://admin:@park0101@172.31.18.162:558/LiveChannel/1/media.smp");

            // 원본 영상
            Mat mat = new Mat();

            while (true)
            {
                try
                {
                    if (!capture.Read(mat))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("접속이상 강제 종료...");
                        Environment.Exit(0);
                    }


                    // 이미지 저장
                    if (save)
                    {
                        mat.SaveImage(@$"c:\test\{DateTime.Now.ToString("yyyyMMddHHmmssfff")}.jpg");
                        save = false;
                    }


                    // 영상 보이기
                    Cv2.ImShow("Show", mat);

                    if (Cv2.WaitKey(1) >= 0) break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            mat.Dispose();
        }

    }

}