using OpenCvSharp;

namespace Lecture01
{
    internal class Program
    {
        public static bool save = false;

        static void Main()
        {
            Task.Run(() =>
            { 
                while (true)
                {
                    var readKey = Console.ReadKey();

                    if (readKey.Key == ConsoleKey.Enter)
                    {
                        Console.WriteLine("저장");
                        save = true;
                    }
                }
            });

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
                        mat.SaveImage(@$"c:\{DateTime.Now.ToString("yyyyMMddHHmmssfff")}.jpg");
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