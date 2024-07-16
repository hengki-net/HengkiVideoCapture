using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Drawing;
using System.Security.Cryptography;

namespace Lecture04
{
    internal class Program
    {
        private static bool _saveSwitch = false;    // 이미지 저장용 스위치. true로 바뀌면 저장되고 false로 되돌아온다.
        private static long _lastCaptureTime = 0;   // 마지막 이미지 저장 시간을 저장. 저장 딜레이를 주기 위한 변수이다.

        static void Main()
        {
            // 비디오 캡처 처리
            ProcessVideoCapture();
        }

        /// <summary>
        /// 
        /// 
        /// 
        /// 비디오 캡처 처리
        /// </summary>
        private static void ProcessVideoCapture()
        {
            // RTSP 영상 가져오기
            VideoCapture capture = new VideoCapture();
            //capture.Open("rtsp://admin:@park0101@172.31.18.162:558/LiveChannel/1/media.smp");
            capture.Open("rtsp://admin:WBF460-8660@172.31.18.158:558/LiveChannel/2/media.smp");

            Mat originalMat = new Mat();
            Mat handlingMat = new Mat();

            while (true)
            {
                try
                {
                    if (!capture.Read(originalMat))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("접속이상 강제 종료...");
                        Environment.Exit(0);
                    }

                    // 이미지 조작
                    HandleingCaptureImage(ref originalMat, out handlingMat);

                    // 픽셀 카운트
                    _saveSwitch = observeImageCapture(ref handlingMat, boundary: 200, red: 100);

                    // 이미지 저장
                    if (_saveSwitch)
                    {
                        //SaveCaptureImageToFile(ref originalMat, @$"c:\{DateTime.Now.ToString("yyyyMMddHHmmssfff")}.jpg");
                        _saveSwitch = false;
                    }

                    // 이미지에 내용 출력
                    printContentOnImage(ref originalMat);


                    // 영상 보이기
                    Cv2.ImShow("Show1", originalMat);
                    //Cv2.ImShow("Show2", handlingMat);

                    if (Cv2.WaitKey(1) >= 0) break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            originalMat.Dispose();

        }

        /// <summary>
        /// 비디오 프레임 이미지 조작
        /// </summary>
        /// <param name="mat"></param>
        private static void HandleingCaptureImage(ref Mat originalMat, out Mat handlingMat)
        {
            // 이미지 자르기
            handlingMat = originalMat.SubMat(new Rect(1780, 400, 50, 100));

            // 그레이스케일
            Cv2.CvtColor(handlingMat, handlingMat, ColorConversionCodes.BGR2GRAY);

            // 가우시안
            Cv2.GaussianBlur(handlingMat, handlingMat, new OpenCvSharp.Size(5, 5), 1, 0, BorderTypes.Default);

            // Edge 검출
            Cv2.Canny(handlingMat, handlingMat, 50, 250);
        }

        /// <summary>
        /// 비디오 프레임 위에 컨텐츠 출력
        /// </summary>
        /// <param name="originalMat"></param>
        private static void printContentOnImage(ref Mat originalMat)
        {
            // 글자쓰기
            Cv2.PutText(originalMat, $"Hello", new OpenCvSharp.Point(10, 30), 
                            HersheyFonts.HersheySimplex, 1, Scalar.LightGreen, 1, LineTypes.AntiAlias);

            // 사각형 그리기
            Cv2.Rectangle(originalMat, new Rect(1780, 400, 50, 100), Scalar.GreenYellow, 1);

        }

        /// <summary>
        /// 비디오 프레임 이미지 저장
        /// </summary>
        /// <param name="mat">이미지 프레임</param>
        /// <param name="path">저장경로</param>
        /// <returns>저장성공시 true, 실패시 false를 출력한다.</returns>
        private static bool SaveCaptureImageToFile(ref Mat mat, string path)
        {
            try
            {
                mat.SaveImage(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 이미지 저장 신호 생성
        /// </summary>
        /// <param name="mat">이미지 프레임</param>
        /// <param name="boundary">경계를 넘는 픽셀 갯수에 따라 캡쳐 유무를 결정한다.</param>
        /// <param name="red">빨간색 경계</param>
        /// <param name="green">녹색 경계</param>
        /// <param name="blue">파란색 경계</param>
        /// <returns>켭쳐 할지를 true/false로 반환한다.</returns>
        private static bool observeImageCapture(ref Mat mat, int boundary = 50, int red = 0, int green = 0, int blue = 0)
        {
            long nowTime = UnixTimeNow();

            if (nowTime - _lastCaptureTime < 10)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("다다닥 저장되는 캡쳐 딜레이..");
                return false;
            }            


            Bitmap bitmap = mat.ToBitmap();
            int pixelCount = 0;

            for (int i = 0; i < bitmap.Width; i++)
            {
                for (int j = 0; j < bitmap.Height; j++)
                {
                    int redValue = Convert.ToInt32(bitmap.GetPixel(i, j).R.ToString());
                    int greenValue = Convert.ToInt32(bitmap.GetPixel(i, j).G.ToString());
                    int blueValue = Convert.ToInt32(bitmap.GetPixel(i, j).B.ToString());

                    if (redValue >= red && greenValue >= green && blueValue >= blue)
                    {
                        pixelCount++;
                    }
                }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"센싱값 : {pixelCount}");

            if (boundary < pixelCount)
            {
                _lastCaptureTime = nowTime;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 유닉스 타임스탬프 (1970년 1월1일 이후의 초)
        /// </summary>
        /// <returns></returns>
        public static long UnixTimeNow()
        {
            var timeSpan = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
            return (long)timeSpan.TotalSeconds;
        }

    }
}