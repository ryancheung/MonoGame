using System;
using Android.Views;
using Android.Text;
using Android.Views.InputMethods;
using Microsoft.Xna.Framework.Input.Touch;
using Android.Widget;

namespace Microsoft.Xna.Framework.Input
{
    public class AndroidImeHandler : ImmService
    {
        private const int IME_FLAG_NO_EXTRACT_UI = 0x10000000;

        private EditText _editText;
        private Button _okButton;
        private MonoGameAndroidGameView _gameView;
        private LinearLayout _inputPanel;

        private Android.Graphics.Point ScreenSize { get { return Game.Activity.ScreenSize; } }

        public override event EventHandler<TextCompositionEventArgs> TextComposition;
        public override event EventHandler<TextInputEventArgs> TextInput;
        public override int VirtualKeyboardHeight { get { return Game.Activity.KeyboardHeight; } }
        public override event EventHandler<InputResultEventArgs> ResultTextUpdated;

        private class EditorActionListener : Java.Lang.Object, TextView.IOnEditorActionListener
        {
            private Action _onDonePressed;

            public EditorActionListener(Action onDonePressed)
            {
                _onDonePressed = onDonePressed;
            }

            public bool OnEditorAction(TextView v, ImeAction actionId, KeyEvent e)
            {
                if (actionId == ImeAction.Done)
                {
                    if (_onDonePressed != null)
                        _onDonePressed();
                }

                return true;
            }
        }

        public AndroidImeHandler(MonoGameAndroidGameView gameView)
        {
            _gameView = gameView;

            _inputPanel = new LinearLayout(Game.Activity);
            _inputPanel.Visibility = ViewStates.Invisible;
            _inputPanel.Orientation = Orientation.Horizontal;
            _inputPanel.SetBackgroundColor(Android.Graphics.Color.White);
            _inputPanel.SetPadding(25, 0, 25, 0);

            _editText = new EditText(Game.Activity);
            _editText.Text = CurrentResultText;
            _editText.SetSingleLine();
            _editText.InputType = InputTypes.ClassText;
            _editText.ImeOptions = (ImeAction)((int)(ImeAction.Done) | IME_FLAG_NO_EXTRACT_UI);
            _editText.SetOnEditorActionListener(new EditorActionListener(StopTextInput));
            _editText.AfterTextChanged += _editText_AfterTextChanged;

            _inputPanel.AddView(_editText, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent, 2));

            _okButton = new Button(Game.Activity) { Text = InputPanelConfirmText ?? "OK" };
            _okButton.SetBackgroundColor(Android.Graphics.Color.Transparent);
            _okButton.Click += _okButton_Click;
            _inputPanel.AddView(_okButton, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent, 1));

            Game.Activity.SoftKeyboardShown += Activity_SoftKeyboardShown;
            Game.Activity.SoftKeyboardHidden += Activity_SoftKeyboardHidden;

            _gameView.ViewTreeObserver.AddOnGlobalLayoutListener(Game.Activity);


            var layoutParams = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            Game.Activity.AddContentView(_inputPanel, layoutParams);
        }

        private void _editText_AfterTextChanged(object sender, AfterTextChangedEventArgs e)
        {
            if (ResultTextUpdated != null)
                ResultTextUpdated.Invoke(this, new InputResultEventArgs(new IMEString(_editText.Text)));
        }

        private void _okButton_Click(object sender, EventArgs e)
        {
            StopTextInput();
        }

        private void Activity_SoftKeyboardShown(object sender, EventArgs e)
        {
            var oldY = _inputPanel.TranslationY;
            _inputPanel.TranslationY = Game.Activity.ScreenSize.Y - _inputPanel.MeasuredHeight - Game.Activity.KeyboardHeight;

            if (oldY != _inputPanel.TranslationY)
                _inputPanel.Parent.RequestLayout();
        }

        private void Activity_SoftKeyboardHidden(object sender, EventArgs e)
        {
            if (!IsTextInputActive) return;


            var oldY = _inputPanel.TranslationY;
            _inputPanel.TranslationY = Game.Activity.ScreenSize.Y - _inputPanel.MeasuredHeight;

            if (oldY != _inputPanel.TranslationY)
                _inputPanel.Parent.RequestLayout();
        }

        public override void StartTextInput()
        {
            if (IsTextInputActive)
                return;

            _inputPanel.RequestFocus();
            _inputPanel.Visibility = ViewStates.Visible;

            _editText.Text = CurrentResultText;
            _editText.SelectAll();

            Game.Activity.InputMethodManager.ShowSoftInput(_editText, ShowFlags.Implicit);
            IsTextInputActive = true;
        }

        public override void StopTextInput()
        {
            if (!IsTextInputActive)
                return;

            if (ResultTextUpdated != null)
                ResultTextUpdated.Invoke(this, new InputResultEventArgs(new IMEString(_editText.Text), true));

            _editText.Text = string.Empty;

            _inputPanel.Visibility = ViewStates.Invisible;
            Game.Activity.InputMethodManager.HideSoftInputFromWindow(_gameView.WindowToken, HideSoftInputFlags.NotAlways);
            IsTextInputActive = false;

            _gameView.RequestFocus();
        }

        const int KeyboardHideOffset = 80;

        internal void Update()
        {
            if (!IsTextInputActive) return;

            TouchCollection touchCollection = TouchPanel.GetState();
            foreach (TouchLocation touchLocation in touchCollection)
            {
                if (TouchLocationState.Pressed == touchLocation.State)
                {
                    if (touchLocation.Position.Y < ((ScreenSize.Y - Game.Activity.KeyboardHeight) - KeyboardHideOffset))
                        StopTextInput();
                }
            }
        }
    }
}
