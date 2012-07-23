﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;
using System.Windows.Media;

namespace ARDroneWPFTestApplication
{
    using Microsoft.Kinect;

    public class CKinect
    {
        public void Connect()
        {
            try
            {
                m_KinectSensor.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

        public void Disconnect()
        {
            try
            {
                m_KinectSensor.Stop();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

        public void EnableSkeletonStream()
        {
            try
            {
                m_KinectSensor.SkeletonStream.Enable();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

        public void DisableSkeletonStream()
        {
            try
            {
                m_KinectSensor.SkeletonStream.Disable();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

        public void ChangeViewAngle(int _Angle)
        {
            try
            {
                m_KinectSensor.ElevationAngle = _Angle;
            }
            catch (System.Exception ex)
            {
            	
            }
           
        }

        public BitmapSource GetSkeletonPictureContext()
        {
            return m_DepthImageSource;
        }

        private KinectSensor m_KinectSensor;

        private Skeleton[]   m_CurrentSkeletons;

        private CARDrone     m_ArDrone;

        private BitmapSource m_DepthImageSource;



        public CKinect(CARDrone _ArDrone)
        {
            m_ArDrone = _ArDrone;

            m_DepthImageSource = null;

            try
            {
                m_KinectSensor = KinectSensor.KinectSensors[0];

                //m_KinectSensor.DepthStream.Enable();

                m_KinectSensor.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(SkeletonFrameReady);

                m_KinectSensor.DepthFrameReady += new EventHandler<DepthImageFrameReadyEventArgs>(DepthFrameReady);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

        private void DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame CurrentDepthImageFrame = e.OpenDepthImageFrame())
            {
                if (CurrentDepthImageFrame != null)
                {
                    short[] DepthImageData = new short[CurrentDepthImageFrame.PixelDataLength];

                    byte[] DepthImageBytes = new byte[CurrentDepthImageFrame.PixelDataLength * 4];

                    CurrentDepthImageFrame.CopyPixelDataTo(DepthImageData);

                    const int BlueColorIndex = 0;

                    const int GreenColorIndex = 0;

                    const int RedColorIndex = 0;

                    for (int IndexOfDepthImage = 0, IndexOfDepthImageBytes = 0; IndexOfDepthImage < DepthImageData.Length && IndexOfDepthImageBytes < DepthImageData.Length * 4; ++IndexOfDepthImage, IndexOfDepthImageBytes += 4)
                    {
                        byte Intensity = (byte)((float)DepthImageData[IndexOfDepthImage] / (float)short.MaxValue * (float)byte.MaxValue);

                        DepthImageBytes[IndexOfDepthImageBytes + BlueColorIndex] = Intensity;

                        DepthImageBytes[IndexOfDepthImageBytes + GreenColorIndex] = Intensity;

                        DepthImageBytes[IndexOfDepthImageBytes + RedColorIndex] = Intensity;
                    }

                    m_DepthImageSource = BitmapSource.Create(CurrentDepthImageFrame.Width, CurrentDepthImageFrame.Height, 96, 96, PixelFormats.Bgr32, null, DepthImageBytes, CurrentDepthImageFrame.Width * 4);
                }

            }
        }

        private void SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame CurrentSkeletonFrame = e.OpenSkeletonFrame())
            {
                if (CurrentSkeletonFrame != null && CurrentSkeletonFrame.SkeletonArrayLength > 0)
                {
                    m_CurrentSkeletons = new Skeleton[CurrentSkeletonFrame.SkeletonArrayLength];

                    CurrentSkeletonFrame.CopySkeletonDataTo(m_CurrentSkeletons);

                    Skeleton CurrentSkeleton = m_CurrentSkeletons[0];

                    if (CurrentSkeleton.TrackingState == SkeletonTrackingState.Tracked)
                    {

                        Joint Head;
                        Joint Hip;
                        Joint HandLeft;
                        Joint HandRight;

                        Head = CurrentSkeleton.Joints[JointType.Head];
                        Hip = CurrentSkeleton.Joints[JointType.HipCenter];
                        HandLeft = CurrentSkeleton.Joints[JointType.HandLeft];
                        HandRight = CurrentSkeleton.Joints[JointType.HandRight];

                        if (Math.Abs(HandLeft.Position.X - HandRight.Position.X) > 2 * Math.Abs(Head.Position.Y - Hip.Position.Y))
                        {
                            //fly position detected
                            if (m_ArDrone.ActualState != CARDrone.State.Fly && m_ArDrone.ActualState != CARDrone.State.Error)
                            {
                                m_ArDrone.TakeOff();
                            }

                            float h = Head.Position.Y - Hip.Position.Y;
                            float n1 = Hip.Position.Z - Head.Position.Z;
                            float n2 = Hip.Position.X - Head.Position.X;

                            float AngleNick = (float)(Math.Atan(n1 / h) * (360 / (2 * Math.PI)));
                            float AngleRoll = (float)(Math.Atan(n2 / h) * (360 / (2 * Math.PI)));

                            float Yaw = HandLeft.Position.Z - HandRight.Position.Z;

                            //Check output of calculated data
                            //Console.WriteLine("Nick: " + AngleNick + "; Roll: " + AngleRoll + "; Yaw: " + Yaw);

                            //very risky but we can test it
                            //m_ArDrone.Fly(angleRoll, angleNick, yaw, 0.0f);

                            //not so risky but must be tested too
                            //it can only be done one of these
                            if (AngleNick > 25)
                            {
                                m_ArDrone.Pitch(1.0f);
                            }
                            else if (AngleNick < 25)
                            {
                                m_ArDrone.Pitch(-1.0f);
                            }
                            else if (AngleRoll > 25)
                            {
                                m_ArDrone.Roll(1.0f);
                            }
                            else if (AngleRoll < 25)
                            {
                                m_ArDrone.Roll(-1.0f);
                            }
                            else if (Yaw > 1.0f)
                            {
                                m_ArDrone.Yaw(1.0f);
                            }
                            else if (Yaw < -1.0f)
                            {
                                m_ArDrone.Yaw(-1.0f);
                            }
                        }
                        else
                        {
                            //land position detected
                            m_ArDrone.Land();
                        }
                    }
                    else
                    {
                        m_ArDrone.Land();
                    }
                }
            }
        }
    }
}