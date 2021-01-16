using CoreGraphics;
using Microsoft.Xna.Framework.Input.Touch;
using System;
using UIKit;

namespace Microsoft.Xna.Framework.iOS.Input
{
    public class iOSImeHandler : ImmService
    {
        private UIWindow mainWindow;
        private UIViewController gameViewController;

        private UIStackView _inputPanel;

        private UIButton _okButton;
        private UITextField _textField;

        private int _virtualKeyboardHeight;

        private CGRect _bounds;

        public const int InputPanelHeight = 40;


        public iOSImeHandler(Game game)
        {
            mainWindow = game.Services.GetService<UIWindow>();
            gameViewController = game.Services.GetService<UIViewController>();

            _bounds = gameViewController.View.Bounds;

            _inputPanel = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Alignment = UIStackViewAlignment.Fill,
                Distribution = UIStackViewDistribution.Fill,
                Spacing = 0,
                Frame = new CGRect(0, _bounds.Height - InputPanelHeight, _bounds.Width, InputPanelHeight)
            };

            var bgView = new UIView(frame: new CGRect(0, 0, _bounds.Width, InputPanelHeight));
            bgView.BackgroundColor = UIColor.White;
            bgView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
            _inputPanel.AddSubview(bgView);

            _textField = new UITextField(new CGRect(0, 0, _bounds.Width - 80, InputPanelHeight));
            _textField.Text = CurrentResultText;
            _textField.KeyboardType = UIKeyboardType.Default;
            _textField.ReturnKeyType = UIReturnKeyType.Done;
            _textField.AutocapitalizationType = UITextAutocapitalizationType.None;
            _textField.ShouldReturn += TextField_ShouldReturn;
            _textField.EditingChanged += (o, e) =>
            {
                if (ResultTextUpdated != null)
                    ResultTextUpdated.Invoke(this, new InputResultEventArgs(new IMEString(_textField.Text)));
            };

            _inputPanel.AddSubview(_textField);

            _okButton = new UIButton(UIButtonType.Plain);
            _okButton.Frame = new CGRect(_bounds.Width - 80, 0, 80, InputPanelHeight);
            _okButton.SetTitle(InputPanelConfirmText ?? "OK", UIControlState.Normal);
            _okButton.SetTitleColor(UIColor.Black, UIControlState.Normal);
            _okButton.SetTitleColor(UIColor.Black, UIControlState.Focused);
            _okButton.BackgroundColor = UIColor.LightGray;
            _okButton.TouchUpInside += (o, e) =>
            {
                StopTextInput();
            };

            _inputPanel.AddSubview(_okButton);

            gameViewController.Add(_inputPanel);
            _inputPanel.Hidden = true;

            UIKeyboard.Notifications.ObserveWillShow((s, e) =>
            {
                _virtualKeyboardHeight = (int)(e.FrameEnd.Height * UIScreen.MainScreen.Scale);
                _inputPanel.Frame = new CGRect(0, _bounds.Height - e.FrameEnd.Height - _inputPanel.Frame.Height, _bounds.Width, InputPanelHeight);
            });

            UIKeyboard.Notifications.ObserveWillHide((s, e) =>
            {
                _virtualKeyboardHeight = 0;
                _inputPanel.Frame = new CGRect(0, _bounds.Height - _inputPanel.Frame.Height, _bounds.Width, InputPanelHeight);
            });
        }

        public override event EventHandler<TextCompositionEventArgs> TextComposition;
        public override event EventHandler<TextInputEventArgs> TextInput;
        public override event EventHandler<InputResultEventArgs> ResultTextUpdated;

        public override int VirtualKeyboardHeight { get { return _virtualKeyboardHeight; } }

        private bool TextField_ShouldReturn(UITextField textfield)
        {
            StopTextInput();

            // We do not want UITextField to insert line-breaks.
            return false;
        }

        public override void StartTextInput()
        {
            if (IsTextInputActive)
                return;

            _okButton.SetTitle(InputPanelConfirmText ?? "OK", UIControlState.Normal);
            _inputPanel.Hidden = false;
            _textField.Text = CurrentResultText;
            _textField.SelectAll(_textField);
            _textField.BecomeFirstResponder();
            IsTextInputActive = true;
        }

        public override void StopTextInput()
        {
            if (!IsTextInputActive)
                return;

            _textField.EndEditing(true);

            if (ResultTextUpdated != null)
                ResultTextUpdated.Invoke(this, new InputResultEventArgs(new IMEString(_textField.Text), true));

            _textField.Text = string.Empty;
            _inputPanel.ResignFirstResponder();
            _inputPanel.Hidden = true;

            gameViewController.View.BecomeFirstResponder();
            IsTextInputActive = false;
        }

        internal void Update()
        {
            if (!IsTextInputActive) return;

            TouchCollection touchCollection = TouchPanel.GetState();
            foreach (TouchLocation touchLocation in touchCollection)
            {
                if (TouchLocationState.Pressed == touchLocation.State)
                {
                    if (touchLocation.Position.Y < ((mainWindow.GetFrame().Height * UIScreen.MainScreen.Scale - _virtualKeyboardHeight) - InputPanelHeight))
                        StopTextInput();
                }
            }
        }
    }
}
