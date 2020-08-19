﻿using System;
using MonoGame.Framework;
using ImeSharp;

namespace Microsoft.Xna.Framework.Input
{
    /// <summary>
    /// Integrate IME to WindowsDX platform with ImeSharp library.
    /// </summary>
    internal sealed class WinFormsImeHandler : ImmService
    {
        private IntPtr _windowHandle;

        public override event EventHandler<TextCompositionEventArgs> TextComposition;
        public override event EventHandler<TextInputEventArgs> TextInput;

        public override void StartTextInput()
        {
            InputMethod.Enabled = true;
            IsTextInputActive = true;
        }

        public override void StopTextInput()
        {
            InputMethod.Enabled = false;
            IsTextInputActive = false;
        }

        public override int VirtualKeyboardHeight { get { return 0; } }

        /// <summary>
        /// Constructor, must be called when the window create.
        /// </summary>
        /// <param name="windowHandle">Handle of the window</param>
        internal WinFormsImeHandler(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;
            InputMethod.Initialize(windowHandle, ShowOSImeWindow);

            InputMethod.TextInputCallback = OnTextInput;
            InputMethod.TextCompositionCallback = OnTextComposition;
        }

        public override void SetTextInputRect(Rectangle rect)
        {
            if (!InputMethod.Enabled)
                return;

            InputMethod.SetTextInputRect(rect.X, rect.Y, rect.Width, rect.Height);
        }

        private void OnTextInput(char charInput)
        {
            var key = Keys.None;
            if (!char.IsSurrogate(charInput))
                key = (Keys)(WinFormsGameWindow.VkKeyScanEx(charInput, System.Windows.Forms.InputLanguage.CurrentInputLanguage.Handle) & 0xff);

            if (TextInput != null)
                TextInput.Invoke(this, new TextInputEventArgs(charInput, key));
        }

        private void OnTextComposition(ImeSharp.IMEString compositionText, int cursorPosition,
            ImeSharp.IMEString[] candidateList, int candidatePageStart, int candidatePageSize, int candidateSelection)
        {
            if (TextComposition == null)
                return;

            IMEString[] candidates = null;
            if (candidateList != null)
            {
                candidates = new IMEString[candidateList.Length];
                for (int i = 0; i < candidateList.Length; i++)
                    candidates[i] = new IMEString(candidateList[i].ToIntPtr());
            }

            TextComposition.Invoke(this, new TextCompositionEventArgs(new IMEString(compositionText.ToIntPtr()), cursorPosition,
                candidates, candidatePageStart, candidatePageSize, candidateSelection));
        }
    }
}
