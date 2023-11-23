using CommunityToolkit.Maui.Layouts;
using CommunityToolkit.Mvvm.Messaging;
using NeuroAccessMaui.Services.Tag;
using Waher.Content.Html.Elements;
using Waher.Runtime.Profiling.Events;

namespace NeuroAccessMaui.UI.Pages.Registration;

public partial class RegistrationPage
{
	public RegistrationPage(RegistrationViewModel ViewModel)
	{
		this.InitializeComponent();
		this.ContentPageModel = ViewModel;

		ViewModel.SetPagesContainer([
			this.LoadingView,
			this.ChoosePurposeView,
			this.ValidatePhoneView,
			this.ValidateEmailView,
			this.ChooseProviderView,
			this.CreateAccountView,
			this.DefinePinView,
		]);

		StateContainer.SetCurrentState(this.GridWithAnimation, "Loading");
	}

	/// <inheritdoc/>
	protected override Task OnAppearingAsync()
	{
		WeakReferenceMessenger.Default.Register<RegistrationPageMessage>(this, this.HandleRegistrationPageMessage);
		WeakReferenceMessenger.Default.Register<KeyboardSizeMessage>(this, this.HandleKeyboardSizeMessage);
		return base.OnAppearingAsync();
	}

	/// <inheritdoc/>
	protected override Task OnDisappearingAsync()
	{
		WeakReferenceMessenger.Default.Unregister<RegistrationPageMessage>(this);
		WeakReferenceMessenger.Default.Unregister<KeyboardSizeMessage>(this);
		return base.OnDisappearingAsync();
	}

	private async void HandleRegistrationPageMessage(object Recipient, RegistrationPageMessage Message)
	{
		RegistrationStep NewStep = Message.Step;

		if (NewStep == RegistrationStep.Complete)
		{
			await App.SetMainPageAsync();
			return;
		}

		string NewState = NewStep switch
		{
			RegistrationStep.RequestPurpose => "ChoosePurpose",
			RegistrationStep.ValidatePhone => "ValidatePhone",
			RegistrationStep.ValidateEmail => "ValidateEmail",
			RegistrationStep.ChooseProvider => "ChooseProvider",
			RegistrationStep.CreateAccount => "CreateAccount",
			//!!! RegistrationStep.RegisterIdentity => "RegisterIdentity",
			RegistrationStep.DefinePin => "DefinePin",
			_ => throw new NotImplementedException(),
		};

		await this.Dispatcher.DispatchAsync(async () =>
		{
			await StateContainer.ChangeStateWithAnimation(this.GridWithAnimation, NewState, CancellationToken.None);

			if (Recipient is RegistrationPage RegistrationPage)
			{
				RegistrationViewModel ViewModel = RegistrationPage.ViewModel<RegistrationViewModel>();
				await ViewModel.DoAssignProperties(NewStep);
			}
		});
	}

	private async void HandleKeyboardSizeMessage(object Recipient, KeyboardSizeMessage Message)
	{
		await this.Dispatcher.DispatchAsync(() =>
		{
			Thickness Margin = new(0, 0, 0, Message.KeyboardSize);
			this.TheMainGrid.Margin = Margin;
		});
	}
}
