﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tobii.Interaction;
using Ozeki.Media;
using Ozeki.Camera;

namespace TobiiEyeTracker
{
    class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            AllocConsole();
            InitializeEyeTracker();
            FreeConsole();
        }
        
        private static void InitializeEyeTracker()
        {
            // Everything starts with initializing Host, which manages the connection to the 
            // Tobii Engine and provides all the Tobii Core SDK functionality.
            // NOTE: Make sure that Tobii.EyeX.exe is running
            var host = new Host();

            PrintSampleIntroText();
            ResizeMainWindow();
            InitializeOnvifCamera();
            CreateGazeAwareZones(host);

            Console.ReadKey();
            
            host.DisableConnection();
        }

        private static void ResizeMainWindow()
        {
            var currentWindowHandle = Process.GetCurrentProcess().MainWindowHandle;
            
//            MoveWindow(currentWindowHandle, (Screen.PrimaryScreen.Bounds.Width - 1280) / 2,
//                (Screen.PrimaryScreen.Bounds.Height - 720) / 2, 1280, 720, true);

            MoveWindow(currentWindowHandle, -5, 0, 1280, 720, true);
        }

        private static VirtualInteractorAgent CreateGazeAwareZones(Host host, VirtualInteractorAgent interactorAgent = null)
        {
            // InteractorAgents are defined per window, so we need a handle to it.
            var currentWindowHandle = Process.GetCurrentProcess().MainWindowHandle;

            // Let's also obtain its bounds using Windows API calls (hidden in a helper method below).
            var currentWindowBounds = GetWindowBounds(currentWindowHandle);

            // Create bounds of window sides.
            var sizeOfSides = 0.2;
            
            var rightSideOfWindow = currentWindowBounds;
            rightSideOfWindow.X = currentWindowBounds.X + (currentWindowBounds.Width - currentWindowBounds.X) * (1 - sizeOfSides);
            rightSideOfWindow.Y = currentWindowBounds.Height + (currentWindowBounds.Y - currentWindowBounds.Height) * (1 - sizeOfSides);
            rightSideOfWindow.Height = currentWindowBounds.Height + (currentWindowBounds.Y - currentWindowBounds.Height) * sizeOfSides;
            
            Console.WriteLine(rightSideOfWindow.ToString());

            var leftSideOfWindow = currentWindowBounds;
            leftSideOfWindow.Width = currentWindowBounds.X + (currentWindowBounds.Width - currentWindowBounds.X) * sizeOfSides;
            leftSideOfWindow.Y = currentWindowBounds.Height + (currentWindowBounds.Y - currentWindowBounds.Height) * (1 - sizeOfSides);
            leftSideOfWindow.Height = currentWindowBounds.Height + (currentWindowBounds.Y - currentWindowBounds.Height) * sizeOfSides;
            
            Console.WriteLine(leftSideOfWindow.ToString());

            var topSideOfWindow = currentWindowBounds;
            topSideOfWindow.Height = currentWindowBounds.Height + (currentWindowBounds.Y - currentWindowBounds.Height) * (1 - sizeOfSides);
            
            Console.WriteLine(topSideOfWindow.ToString());

            var bottomSideOfWindow = currentWindowBounds;
            bottomSideOfWindow.Y = currentWindowBounds.Height + (currentWindowBounds.Y - currentWindowBounds.Height) * sizeOfSides;
            
            Console.WriteLine(bottomSideOfWindow.ToString());

            // Remove Interactors from InteractorAgent if it exists and create it if it doesn't.
            if (interactorAgent != null)
            {
                foreach (Interactor i in interactorAgent.Interactors)
                {
                    interactorAgent.RemoveInteractor(i.Id);
                }
            }
            else
            {
                interactorAgent = host.InitializeVirtualInteractorAgent(currentWindowHandle, "ConsoleWindowAgent");
            }
            
            // Next we are going to create an interactor, which we will define with the gaze aware behavior.
            // Gaze aware behavior simply tells you whether somebody is looking at the interactor or not.
            interactorAgent
                .AddInteractorFor(rightSideOfWindow)
                .WithGazeAware()
                .HasGaze(() =>
                {
                    Console.WriteLine("Gaze found. Right.");
                    _camera.CameraMovement.ContinuousMove(MoveDirection.Right);
                })
                .LostGaze(() =>
                {
                    Console.WriteLine("Gaze lost. Right.");
                    _camera.CameraMovement.StopMovement();
                });
            interactorAgent
                .AddInteractorFor(leftSideOfWindow)
                .WithGazeAware()
                .HasGaze(() =>
                {
                    Console.WriteLine("Gaze found. Left.");
                    _camera.CameraMovement.ContinuousMove(MoveDirection.Left);
                })
                .LostGaze(() =>
                {
                    Console.WriteLine("Gaze lost. Left.");
                    _camera.CameraMovement.StopMovement();
                });
            interactorAgent
                .AddInteractorFor(topSideOfWindow)
                .WithGazeAware()
                .HasGaze(() =>
                {
                    Console.WriteLine("Gaze found. Top.");
                    _camera.CameraMovement.ContinuousMove(MoveDirection.Up);
                })
                .LostGaze(() =>
                {
                    Console.WriteLine("Gaze lost. Top.");
                    _camera.CameraMovement.StopMovement();
                });
            interactorAgent
                .AddInteractorFor(bottomSideOfWindow)
                .WithGazeAware()
                .HasGaze(() =>
                {
                    Console.WriteLine("Gaze found. Bottom.");
                    _camera.CameraMovement.ContinuousMove(MoveDirection.Down);
                })
                .LostGaze(() =>
                {
                    Console.WriteLine("Gaze lost. Bottom.");
                    _camera.CameraMovement.StopMovement();
                });

            return interactorAgent;
        }
        
        private static IIPCamera _camera;
        private static string cameraAddress = "";
        private static string cameraUser = "";
        private static string cameraPassword = "";
        
        private static void InitializeOnvifCamera()
        {
            _camera = new IPCamera(cameraAddress, cameraUser, cameraPassword);
            _camera.Start();
            Console.WriteLine(_camera.State.ToString());
        }

        #region Helpers

        private static void PrintSampleIntroText()
        {
            Console.Clear();
            Console.WriteLine("Look at the window to trigger HasGaze event and look away to trigger LostGaze event.");
            Console.WriteLine();
        }

        private static Tobii.Interaction.Rectangle GetWindowBounds(IntPtr windowHandle)
        {
            NativeRect nativeNativeRect;

            if (GetWindowRect(windowHandle, out nativeNativeRect))
            {
                return new Tobii.Interaction.Rectangle
                {
                    X = nativeNativeRect.Left,
                    Y = nativeNativeRect.Top,
                    Width = nativeNativeRect.Right,
                    Height = nativeNativeRect.Bottom
                };
            }

            return new Tobii.Interaction.Rectangle(0d, 0d, 1000d, 1000d);
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hWnd, out NativeRect nativeRect);
        
        [StructLayout(LayoutKind.Sequential)]
        public struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FreeConsole();

        #endregion
    }
}