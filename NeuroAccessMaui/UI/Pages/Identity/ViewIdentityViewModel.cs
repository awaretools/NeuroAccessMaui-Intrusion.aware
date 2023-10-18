using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.UI.Pages.Main.Security;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Data;
using NeuroAccessMaui.Services.UI.Photos;
using Waher.Content.Xml;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;

namespace NeuroAccessMaui.UI.Pages.Identity;

/// <summary>
/// The view model to bind to for when displaying identities.
/// </summary>
public partial class ViewIdentityViewModel : QrXmppViewModel
{
	private readonly PhotosLoader photosLoader;
	private LegalIdentity requestorIdentity;
	private string requestorFullJid;
	private string signatoryIdentityId;
	private string petitionId;
	private string purpose;
	private byte[] contentToSign;

	/// <summary>
	/// Creates an instance of the <see cref="ViewIdentityViewModel"/> class.
	/// </summary>
	public ViewIdentityViewModel()
	{
		this.photosLoader = new PhotosLoader(this.Photos);
	}

	/// <inheritdoc/>
	protected override async Task OnInitialize()
	{
		await base.OnInitialize();

		if (ServiceRef.NavigationService.TryGetArgs(out ViewIdentityNavigationArgs? Args))
		{
			this.LegalIdentity = Args.Identity ?? ServiceRef.TagProfile.LegalIdentity;
			this.requestorIdentity = Args.RequestorIdentity;
			this.requestorFullJid = Args.RequestorFullJid;
			this.signatoryIdentityId = Args.SignatoryIdentityId;
			this.petitionId = Args.PetitionId;
			this.purpose = Args.Purpose;
			this.contentToSign = Args.ContentToSign;
		}

		if (this.LegalIdentity is null)
		{
			this.LegalIdentity = ServiceRef.TagProfile.LegalIdentity;
			this.requestorIdentity = null;
			this.requestorFullJid = null;
			this.signatoryIdentityId = null;
			this.petitionId = null;
			this.purpose = null;
			this.contentToSign = null;
			this.IsPersonal = true;
		}
		else
		{
			this.IsPersonal = false;
		}

		this.AssignProperties();

		if (this.ThirdParty)
		{
			ContactInfo Info = await ContactInfo.FindByBareJid(this.BareJid);

			if ((Info is not null) &&
				(Info.LegalIdentity is null ||
				(Info.LegalId != this.LegalId &&
				Info.LegalIdentity.Created < this.LegalIdentity.Created &&
				this.LegalIdentity.State == IdentityState.Approved)))
			{
				Info.LegalId = this.LegalId;
				Info.LegalIdentity = this.LegalIdentity;
				Info.FriendlyName = ContactInfo.GetFriendlyName(this.LegalIdentity);

				await Database.Update(Info);
				await Database.Provider.Flush();
			}
		}

		ServiceRef.TagProfile.Changed += this.TagProfile_Changed;
		ServiceRef.XmppService.LegalIdentityChanged += this.SmartContracts_LegalIdentityChanged;
	}

	/// <inheritdoc/>
	protected override async Task OnDispose()
	{
		this.photosLoader.CancelLoadPhotos();

		ServiceRef.TagProfile.Changed -= this.TagProfile_Changed;
		ServiceRef.XmppService.LegalIdentityChanged -= this.SmartContracts_LegalIdentityChanged;

		this.LegalIdentity = null;

		await base.OnDispose();
	}

	private void AssignProperties()
	{
		this.Created = this.LegalIdentity?.Created ?? DateTime.MinValue;
		this.Updated = this.LegalIdentity?.Updated.GetDateOrNullIfMinValue();
		this.LegalId = this.LegalIdentity?.Id;

		if (this.requestorIdentity is not null)
		{
			this.BareJid = this.requestorIdentity.GetJid(Constants.NotAvailableValue);
		}
		else if (this.LegalIdentity is not null)
		{
			this.BareJid = this.LegalIdentity.GetJid(Constants.NotAvailableValue);
		}
		else
		{
			this.BareJid = Constants.NotAvailableValue;
		}

		if (this.LegalIdentity?.ClientPubKey is not null)
		{
			this.PublicKey = Convert.ToBase64String(this.LegalIdentity.ClientPubKey);
		}
		else
		{
			this.PublicKey = string.Empty;
		}

		this.State = this.LegalIdentity?.State ?? IdentityState.Rejected;
		this.From = this.LegalIdentity?.From.GetDateOrNullIfMinValue();
		this.To = this.LegalIdentity?.To.GetDateOrNullIfMinValue();

		if (this.LegalIdentity is not null)
		{
			this.FirstName = this.LegalIdentity[Constants.XmppProperties.FirstName];
			this.MiddleNames = this.LegalIdentity[Constants.XmppProperties.MiddleName];
			this.LastNames = this.LegalIdentity[Constants.XmppProperties.LastName];
			this.PersonalNumber = this.LegalIdentity[Constants.XmppProperties.PersonalNumber];
			this.Address = this.LegalIdentity[Constants.XmppProperties.Address];
			this.Address2 = this.LegalIdentity[Constants.XmppProperties.Address2];
			this.ZipCode = this.LegalIdentity[Constants.XmppProperties.ZipCode];
			this.Area = this.LegalIdentity[Constants.XmppProperties.Area];
			this.City = this.LegalIdentity[Constants.XmppProperties.City];
			this.Region = this.LegalIdentity[Constants.XmppProperties.Region];
			this.CountryCode = this.LegalIdentity[Constants.XmppProperties.Country];
			this.Country = ISO_3166_1.ToName(this.CountryCode);
			this.OrgName = this.LegalIdentity[Constants.XmppProperties.OrgName];
			this.OrgNumber = this.LegalIdentity[Constants.XmppProperties.OrgNumber];
			this.OrgDepartment = this.LegalIdentity[Constants.XmppProperties.OrgDepartment];
			this.OrgRole = this.LegalIdentity[Constants.XmppProperties.OrgRole];
			this.OrgAddress = this.LegalIdentity[Constants.XmppProperties.OrgAddress];
			this.OrgAddress2 = this.LegalIdentity[Constants.XmppProperties.OrgAddress2];
			this.OrgZipCode = this.LegalIdentity[Constants.XmppProperties.OrgZipCode];
			this.OrgArea = this.LegalIdentity[Constants.XmppProperties.OrgArea];
			this.OrgCity = this.LegalIdentity[Constants.XmppProperties.OrgCity];
			this.OrgRegion = this.LegalIdentity[Constants.XmppProperties.OrgRegion];
			this.OrgCountryCode = this.LegalIdentity[Constants.XmppProperties.OrgCountry];
			this.OrgCountry = ISO_3166_1.ToName(this.OrgCountryCode);
			this.HasOrg =
				!string.IsNullOrEmpty(this.OrgName) ||
				!string.IsNullOrEmpty(this.OrgNumber) ||
				!string.IsNullOrEmpty(this.OrgDepartment) ||
				!string.IsNullOrEmpty(this.OrgRole) ||
				!string.IsNullOrEmpty(this.OrgAddress) ||
				!string.IsNullOrEmpty(this.OrgAddress2) ||
				!string.IsNullOrEmpty(this.OrgZipCode) ||
				!string.IsNullOrEmpty(this.OrgArea) ||
				!string.IsNullOrEmpty(this.OrgCity) ||
				!string.IsNullOrEmpty(this.OrgRegion) ||
				!string.IsNullOrEmpty(this.OrgCountryCode) ||
				!string.IsNullOrEmpty(this.OrgCountry);
			this.PhoneNr = this.LegalIdentity[Constants.XmppProperties.Phone];
			this.EMail = this.LegalIdentity[Constants.XmppProperties.EMail];
		}
		else
		{
			this.FirstName = string.Empty;
			this.MiddleNames = string.Empty;
			this.LastNames = string.Empty;
			this.PersonalNumber = string.Empty;
			this.Address = string.Empty;
			this.Address2 = string.Empty;
			this.ZipCode = string.Empty;
			this.Area = string.Empty;
			this.City = string.Empty;
			this.Region = string.Empty;
			this.CountryCode = string.Empty;
			this.Country = string.Empty;
			this.OrgName = Constants.NotAvailableValue;
			this.OrgNumber = Constants.NotAvailableValue;
			this.OrgDepartment = Constants.NotAvailableValue;
			this.OrgRole = Constants.NotAvailableValue;
			this.OrgAddress = Constants.NotAvailableValue;
			this.OrgAddress2 = Constants.NotAvailableValue;
			this.OrgZipCode = Constants.NotAvailableValue;
			this.OrgArea = Constants.NotAvailableValue;
			this.OrgCity = Constants.NotAvailableValue;
			this.OrgRegion = Constants.NotAvailableValue;
			this.OrgCountryCode = Constants.NotAvailableValue;
			this.OrgCountry = Constants.NotAvailableValue;
			this.HasOrg = false;
			this.PhoneNr = string.Empty;
			this.EMail = string.Empty;
		}

		this.IsApproved = this.LegalIdentity?.State == IdentityState.Approved;
		this.IsCreated = this.LegalIdentity?.State == IdentityState.Created;

		this.IsForReview = this.requestorIdentity is not null;
		this.ThirdParty = (this.LegalIdentity is not null) && !this.IsPersonal;

		this.IsForReviewFirstName = !string.IsNullOrWhiteSpace(this.FirstName) && this.IsForReview;
		this.IsForReviewMiddleNames = !string.IsNullOrWhiteSpace(this.MiddleNames) && this.IsForReview;
		this.IsForReviewLastNames = !string.IsNullOrWhiteSpace(this.LastNames) && this.IsForReview;
		this.IsForReviewPersonalNumber = !string.IsNullOrWhiteSpace(this.PersonalNumber) && this.IsForReview;
		this.IsForReviewAddress = !string.IsNullOrWhiteSpace(this.Address) && this.IsForReview;
		this.IsForReviewAddress2 = !string.IsNullOrWhiteSpace(this.Address2) && this.IsForReview;
		this.IsForReviewCity = !string.IsNullOrWhiteSpace(this.City) && this.IsForReview;
		this.IsForReviewZipCode = !string.IsNullOrWhiteSpace(this.ZipCode) && this.IsForReview;
		this.IsForReviewArea = !string.IsNullOrWhiteSpace(this.Area) && this.IsForReview;
		this.IsForReviewRegion = !string.IsNullOrWhiteSpace(this.Region) && this.IsForReview;
		this.IsForReviewCountry = !string.IsNullOrWhiteSpace(this.Country) && this.IsForReview;

		this.IsForReviewOrgName = !string.IsNullOrWhiteSpace(this.OrgName) && this.IsForReview;
		this.IsForReviewOrgNumber = !string.IsNullOrWhiteSpace(this.OrgNumber) && this.IsForReview;
		this.IsForReviewOrgDepartment = !string.IsNullOrWhiteSpace(this.OrgDepartment) && this.IsForReview;
		this.IsForReviewOrgRole = !string.IsNullOrWhiteSpace(this.OrgRole) && this.IsForReview;
		this.IsForReviewOrgAddress = !string.IsNullOrWhiteSpace(this.OrgAddress) && this.IsForReview;
		this.IsForReviewOrgAddress2 = !string.IsNullOrWhiteSpace(this.OrgAddress2) && this.IsForReview;
		this.IsForReviewOrgCity = !string.IsNullOrWhiteSpace(this.OrgCity) && this.IsForReview;
		this.IsForReviewOrgZipCode = !string.IsNullOrWhiteSpace(this.OrgZipCode) && this.IsForReview;
		this.IsForReviewOrgArea = !string.IsNullOrWhiteSpace(this.OrgArea) && this.IsForReview;
		this.IsForReviewOrgRegion = !string.IsNullOrWhiteSpace(this.OrgRegion) && this.IsForReview;
		this.IsForReviewOrgCountry = !string.IsNullOrWhiteSpace(this.OrgCountry) && this.IsForReview;

		// QR
		if (this.LegalIdentity is null)
		{
			this.RemoveQrCode();
		}
		else
		{
			this.GenerateQrCode(Constants.UriSchemes.CreateIdUri(this.LegalIdentity.Id));
		}

		if (this.IsConnected)
		{
			this.ReloadPhotos();
		}
	}

	/// <inheritdoc/>
	protected override Task XmppService_ConnectionStateChanged(object Sender, XmppState NewState)
	{
		return MainThread.InvokeOnMainThreadAsync(async () =>
		{
			await base.XmppService_ConnectionStateChanged(Sender, NewState);

			this.OnPropertyChanged(nameof(this.CanExecuteCommands));

			if (this.IsConnected)
			{
				await Task.Delay(Constants.Timeouts.XmppInit);
				this.ReloadPhotos();
			}
		});
	}

	private async void ReloadPhotos()
	{
		try
		{
			this.photosLoader.CancelLoadPhotos();

			Attachment[] Attachments;

			if (this.requestorIdentity?.Attachments is not null)
			{
				Attachments = this.requestorIdentity.Attachments;
			}
			else
			{
				Attachments = this.LegalIdentity?.Attachments;
			}

			if (Attachments is not null)
			{
				Photo First = await this.photosLoader.LoadPhotos(Attachments, SignWith.LatestApprovedIdOrCurrentKeys);

				this.FirstPhotoSource = First?.Source;
				this.FirstPhotoRotation = First?.Rotation ?? 0;
			}
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);
		}
	}

	private void TagProfile_Changed(object? Sender, PropertyChangedEventArgs e)
	{
		MainThread.BeginInvokeOnMainThread(this.AssignProperties);
	}

	private Task SmartContracts_LegalIdentityChanged(object Sender, LegalIdentityEventArgs e)
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
			if (this.LegalIdentity?.Id == e.Identity.Id)
			{
				this.LegalIdentity = e.Identity;
				this.AssignProperties();
			}
		});

		return Task.CompletedTask;
	}

	#region Properties

	/// <summary>
	/// Holds a list of photos associated with this identity.
	/// </summary>
	public ObservableCollection<Photo> Photos { get; } = new();

	/// <summary>
	/// The full legal identity of the identity
	/// </summary>
	public LegalIdentity LegalIdentity { get; private set; }

	/// <summary>
	/// Created time stamp of the identity
	/// </summary>
	[ObservableProperty]
	private DateTime created;

	/// <summary>
	/// Updated time stamp of the identity
	/// </summary>
	[ObservableProperty]
	private DateTime? updated;

	/// <summary>
	/// Legal id of the identity
	/// </summary>
	[ObservableProperty]
	private string legalId;

	/// <summary>
	/// Bare Jid of the identity
	/// </summary>
	[ObservableProperty]
	private string bareJid;

	/// <summary>
	/// Public key of the identity's signature.
	/// </summary>
	[ObservableProperty]
	private string publicKey;

	/// <summary>
	/// Current state of the identity
	/// </summary>
	[ObservableProperty]
	private IdentityState state;

	/// <summary>
	/// From date (validity range) of the identity
	/// </summary>
	[ObservableProperty]
	private DateTime? from;

	/// <summary>
	/// To date (validity range) of the identity
	/// </summary>
	[ObservableProperty]
	private DateTime? to;

	/// <summary>
	/// First name of the identity
	/// </summary>
	[ObservableProperty]
	private string firstName;

	/// <summary>
	/// Middle name(s) of the identity
	/// </summary>
	[ObservableProperty]
	private string middleNames;

	/// <summary>
	/// Last name(s) of the identity
	/// </summary>
	[ObservableProperty]
	private string lastNames;

	/// <summary>
	/// Personal number of the identity
	/// </summary>
	[ObservableProperty]
	private string personalNumber;

	/// <summary>
	/// Address of the identity
	/// </summary>
	[ObservableProperty]
	private string address;

	/// <summary>
	/// Address (line 2) of the identity
	/// </summary>
	[ObservableProperty]
	private string address2;

	/// <summary>
	/// Zip code of the identity
	/// </summary>
	[ObservableProperty]
	private string zipCode;

	/// <summary>
	/// Area of the identity
	/// </summary>
	[ObservableProperty]
	private string area;

	/// <summary>
	/// City of the identity
	/// </summary>
	[ObservableProperty]
	private string city;

	/// <summary>
	/// Region of the identity
	/// </summary>
	[ObservableProperty]
	private string region;

	/// <summary>
	/// Country of the identity
	/// </summary>
	[ObservableProperty]
	private string country;

	/// <summary>
	/// Country code of the identity
	/// </summary>
	[ObservableProperty]
	private string countryCode;

	/// <summary>
	/// The legal identity's organization name property
	/// </summary>
	[ObservableProperty]
	private string orgName;

	/// <summary>
	/// The legal identity's organization number property
	/// </summary>
	[ObservableProperty]
	private string orgNumber;

	/// <summary>
	/// The legal identity's organization department property
	/// </summary>
	[ObservableProperty]
	private string orgDepartment;

	/// <summary>
	/// The legal identity's organization role property
	/// </summary>
	[ObservableProperty]
	private string orgRole;

	/// <summary>
	/// The legal identity's organization address property
	/// </summary>
	[ObservableProperty]
	private string orgAddress;

	/// <summary>
	/// The legal identity's organization address line 2 property
	/// </summary>
	[ObservableProperty]
	private string orgAddress2;

	/// <summary>
	/// The legal identity's organization zip code property
	/// </summary>
	[ObservableProperty]
	private string orgZipCode;

	/// <summary>
	/// The legal identity's organization area property
	/// </summary>
	[ObservableProperty]
	private string orgArea;

	/// <summary>
	/// The legal identity's organization city property
	/// </summary>
	[ObservableProperty]
	private string orgCity;

	/// <summary>
	/// The legal identity's organization region property
	/// </summary>
	[ObservableProperty]
	private string orgRegion;

	/// <summary>
	/// The legal identity's organization country code property
	/// </summary>
	[ObservableProperty]
	private string orgCountryCode;

	/// <summary>
	/// The legal identity's organization country property
	/// </summary>
	[ObservableProperty]
	private string orgCountry;

	/// <summary>
	/// If organization information is available.
	/// </summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(OrgRowHeight))]
	private bool hasOrg;

	/// <summary>
	/// If organization information is available.
	/// </summary>
	public GridLength OrgRowHeight => this.HasOrg ? GridLength.Auto : new GridLength(0, GridUnitType.Absolute);

	/// <summary>
	/// Country code of the identity
	/// </summary>
	[ObservableProperty]
	private string phoneNr;

	/// <summary>
	/// Country code of the identity
	/// </summary>
	[ObservableProperty]
	private string eMail;

	/// <summary>
	/// Gets or sets whether the identity is approved or not.
	/// </summary>
	[ObservableProperty]
	private bool isApproved;

	/// <summary>
	/// Gets or sets created state of the identity, i.e. if it has been created or not.
	/// </summary>
	[ObservableProperty]
	private bool isCreated;

	/// <summary>
	/// Gets or sets whether the identity is for review or not. This property has its inverse in <see cref="IsNotForReview"/>.
	/// </summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsNotForReview))]
	private bool isForReview;

	/// <summary>
	/// Gets or sets whether the identity is for review or not. This property has its inverse in <see cref="IsForReview"/>.
	/// </summary>
	public bool IsNotForReview => !this.IsForReview;

	/// <summary>
	/// Gets or sets whether the identity is for review or not. This property has its inverse in <see cref="IsForReview"/>.
	/// </summary>
	[ObservableProperty]
	private bool thirdParty;

	/// <summary>
	/// Gets or sets whether the identity is a personal identity.
	/// </summary>
	[ObservableProperty]
	private bool isPersonal;

	/// <summary>
	/// Gets or sets whether the <see cref="FirstName"/> property is checked (when being reviewed)
	/// </summary>
	[ObservableProperty]
	private bool firstNameIsChecked;

	/// <summary>
	/// Gets or sets whether the <see cref="MiddleNames"/> property is checked (when being reviewed)
	/// </summary>
	[ObservableProperty]
	private bool middleNamesIsChecked;

	/// <summary>
	/// Gets or sets whether the <see cref="LastNames"/> property is checked (when being reviewed)
	/// </summary>
	[ObservableProperty]
	private bool lastNamesIsChecked;

	/// <summary>
	/// Gets or sets whether the <see cref="PersonalNumber"/> property is checked (when being reviewed)
	/// </summary>
	[ObservableProperty]
	private bool personalNumberIsChecked;

	/// <summary>
	/// Gets or sets whether the <see cref="Address"/> property is checked (when being reviewed)
	/// </summary>
	[ObservableProperty]
	private bool addressIsChecked;

	/// <summary>
	/// Gets or sets whether the <see cref="Address2"/> property is checked (when being reviewed)
	/// </summary>
	[ObservableProperty]
	private bool address2IsChecked;

	/// <summary>
	/// Gets or sets whether the <see cref="ZipCode"/> property is checked (when being reviewed)
	/// </summary>
	[ObservableProperty]
	private bool zipCodeIsChecked;

	/// <summary>
	/// Gets or sets whether the <see cref="Area"/> property is checked (when being reviewed)
	/// </summary>
	[ObservableProperty]
	private bool areaIsChecked;

	/// <summary>
	/// Gets or sets whether the <see cref="City"/> property is checked (when being reviewed)
	/// </summary>
	[ObservableProperty]
	private bool cityIsChecked;

	/// <summary>
	/// Gets or sets whether the <see cref="Region"/> property is checked (when being reviewed)
	/// </summary>
	[ObservableProperty]
	private bool regionIsChecked;

	/// <summary>
	/// Gets or sets whether the <see cref="CountryCode"/> property is checked (when being reviewed)
	/// </summary>
	[ObservableProperty]
	private bool countryCodeIsChecked;

	/// <summary>
	/// Gets or sets whether the <see cref="OrgName"/> property is checked (when being reviewed)
	/// </summary>
	[ObservableProperty]
	private bool orgNameIsChecked;

	/// <summary>
	/// Gets or sets whether the <see cref="OrgDepartment"/> property is checked (when being reviewed)
	/// </summary>
	[ObservableProperty]
	private bool orgDepartmentIsChecked;

	/// <summary>
	/// Gets or sets whether the <see cref="OrgRole"/> property is checked (when being reviewed)
	/// </summary>
	[ObservableProperty]
	private bool orgRoleIsChecked;

	/// <summary>
	/// Gets or sets whether the <see cref="OrgNumber"/> property is checked (when being reviewed)
	/// </summary>
	[ObservableProperty]
	private bool orgNumberIsChecked;

	/// <summary>
	/// Gets or sets whether the <see cref="OrgAddress"/> property is checked (when being reviewed)
	/// </summary>
	[ObservableProperty]
	private bool orgAddressIsChecked;

	/// <summary>
	/// Gets or sets whether the <see cref="OrgAddress2"/> property is checked (when being reviewed)
	/// </summary>
	[ObservableProperty]
	private bool orgAddress2IsChecked;

	/// <summary>
	/// Gets or sets whether the <see cref="OrgZipCode"/> property is checked (when being reviewed)
	/// </summary>
	[ObservableProperty]
	private bool orgZipCodeIsChecked;

	/// <summary>
	/// Gets or sets whether the <see cref="OrgArea"/> property is checked (when being reviewed)
	/// </summary>
	[ObservableProperty]
	private bool orgAreaIsChecked;

	/// <summary>
	/// Gets or sets whether the <see cref="OrgCity"/> property is checked (when being reviewed)
	/// </summary>
	[ObservableProperty]
	private bool orgCityIsChecked;

	/// <summary>
	/// Gets or sets whether the <see cref="OrgRegion"/> property is checked (when being reviewed)
	/// </summary>
	[ObservableProperty]
	private bool orgRegionIsChecked;

	/// <summary>
	/// Gets or sets whether the <see cref="OrgCountryCode"/> property is checked (when being reviewed)
	/// </summary>
	[ObservableProperty]
	private bool orgCountryCodeIsChecked;

	/// <summary>
	/// Gets or sets whether the Careful Review property is checked (when being reviewed)
	/// </summary>
	[ObservableProperty]
	private bool carefulReviewIsChecked;

	/// <summary>
	/// Gets or sets whether the ApprovePii property is checked (when being reviewed)
	/// </summary>
	[ObservableProperty]
	private bool approvePiiIsChecked;

	/// <summary>
	/// Gets or sets whether the <see cref="FirstName"/> property is for review.
	/// </summary>
	[ObservableProperty]
	private bool isForReviewFirstName;

	/// <summary>
	/// Gets or sets whether the <see cref="MiddleNames"/> property is for review.
	/// </summary>
	[ObservableProperty]
	private bool isForReviewMiddleNames;

	/// <summary>
	/// Gets or sets whether the <see cref="LastNames"/> property is for review.
	/// </summary>
	[ObservableProperty]
	private bool isForReviewLastNames;

	/// <summary>
	/// Gets or sets whether the <see cref="PersonalNumber"/> property is for review.
	/// </summary>
	[ObservableProperty]
	private bool isForReviewPersonalNumber;

	/// <summary>
	/// Gets or sets whether the <see cref="Address"/> property is for review.
	/// </summary>
	[ObservableProperty]
	private bool isForReviewAddress;

	/// <summary>
	/// Gets or sets whether the <see cref="Address2"/> property is for review.
	/// </summary>
	[ObservableProperty]
	private bool isForReviewAddress2;

	/// <summary>
	/// Gets or sets whether the <see cref="City"/> property is for review.
	/// </summary>
	[ObservableProperty]
	private bool isForReviewCity;

	/// <summary>
	/// Gets or sets whether the <see cref="ZipCode"/> property is for review.
	/// </summary>
	[ObservableProperty]
	private bool isForReviewZipCode;

	/// <summary>
	/// Gets or sets whether the <see cref="Area"/> property is for review.
	/// </summary>
	[ObservableProperty]
	private bool isForReviewArea;

	/// <summary>
	/// Gets or sets whether the <see cref="Region"/> property is for review.
	/// </summary>
	[ObservableProperty]
	private bool isForReviewRegion;

	/// <summary>
	/// Gets or sets whether the <see cref="Country"/> property is for review.
	/// </summary>
	[ObservableProperty]
	private bool isForReviewCountry;

	/// <summary>
	/// Gets or sets whether the <see cref="OrgName"/> property is for review.
	/// </summary>
	[ObservableProperty]
	private bool isForReviewOrgName;

	/// <summary>
	/// Gets or sets whether the <see cref="OrgDepartment"/> property is for review.
	/// </summary>
	[ObservableProperty]
	private bool isForReviewOrgDepartment;

	/// <summary>
	/// Gets or sets whether the <see cref="OrgRole"/> property is for review.
	/// </summary>
	[ObservableProperty]
	private bool isForReviewOrgRole;

	/// <summary>
	/// Gets or sets whether the <see cref="OrgNumber"/> property is for review.
	/// </summary>
	[ObservableProperty]
	private bool isForReviewOrgNumber;

	/// <summary>
	/// Gets or sets whether the <see cref="OrgAddress"/> property is for review.
	/// </summary>
	[ObservableProperty]
	private bool isForReviewOrgAddress;

	/// <summary>
	/// Gets or sets whether the <see cref="OrgAddress2"/> property is for review.
	/// </summary>
	[ObservableProperty]
	private bool isForReviewOrgAddress2;

	/// <summary>
	/// Gets or sets whether the <see cref="OrgCity"/> property is for review.
	/// </summary>
	[ObservableProperty]
	private bool isForReviewOrgCity;

	/// <summary>
	/// Gets or sets whether the <see cref="OrgZipCode"/> property is for review.
	/// </summary>
	[ObservableProperty]
	private bool isForReviewOrgZipCode;

	/// <summary>
	/// Gets or sets whether the <see cref="OrgArea"/> property is for review.
	/// </summary>
	[ObservableProperty]
	private bool isForReviewOrgArea;

	/// <summary>
	/// Gets or sets whether the <see cref="OrgRegion"/> property is for review.
	/// </summary>
	[ObservableProperty]
	private bool isForReviewOrgRegion;

	/// <summary>
	/// Gets or sets whether the <see cref="OrgCountry"/> property is for review.
	/// </summary>
	[ObservableProperty]
	private bool isForReviewOrgCountry;

	/// <summary>
	/// Image source of the first photo in the identity.
	/// </summary>
	[ObservableProperty]
	private ImageSource firstPhotoSource;

	/// <summary>
	/// Rotation of the first photo in the identity.
	/// </summary>
	[ObservableProperty]
	private int firstPhotoRotation;

	/// <summary>
	/// Used to find out if an ICommand can execute
	/// </summary>
	public bool CanExecuteCommands => !this.IsBusy && this.IsConnected;

	#endregion

	private void SetIsBusy(bool IsBusy)
	{
		this.IsBusy = IsBusy;
		this.OnPropertyChanged(nameof(this.CanExecuteCommands));
	}

	/// <summary>
	/// Copies Item to clipboard
	/// </summary>
	[RelayCommand]
	private async Task CopyToClipboard(object Item)
	{
		try
		{
			this.SetIsBusy(true);

			if (Item is string Label)
			{
				if (Label == this.LegalId)
				{
					await Clipboard.SetTextAsync(Constants.UriSchemes.IotId + ":" + this.LegalId);
					await ServiceRef.UiSerializer.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
						ServiceRef.Localizer[nameof(AppResources.IdCopiedSuccessfully)]);
				}
				else
				{
					await Clipboard.SetTextAsync(Label);
					await ServiceRef.UiSerializer.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
						ServiceRef.Localizer[nameof(AppResources.TagValueCopiedToClipboard)]);
				}
			}
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);
			await ServiceRef.UiSerializer.DisplayException(ex);
		}
		finally
		{
			this.SetIsBusy(false);
		}
	}

	[RelayCommand(CanExecute = nameof(CanExecuteCommands))]
	private async Task Approve()
	{
		if (this.requestorIdentity is null)
		{
			return;
		}

		try
		{
			if ((!string.IsNullOrEmpty(this.FirstName) && !this.FirstNameIsChecked) ||
				(!string.IsNullOrEmpty(this.MiddleNames) && !this.MiddleNamesIsChecked) ||
				(!string.IsNullOrEmpty(this.LastNames) && !this.LastNamesIsChecked) ||
				(!string.IsNullOrEmpty(this.PersonalNumber) && !this.PersonalNumberIsChecked) ||
				(!string.IsNullOrEmpty(this.Address) && !this.AddressIsChecked) ||
				(!string.IsNullOrEmpty(this.Address2) && !this.Address2IsChecked) ||
				(!string.IsNullOrEmpty(this.ZipCode) && !this.ZipCodeIsChecked) ||
				(!string.IsNullOrEmpty(this.Area) && !this.AreaIsChecked) ||
				(!string.IsNullOrEmpty(this.City) && !this.CityIsChecked) ||
				(!string.IsNullOrEmpty(this.Region) && !this.RegionIsChecked) ||
				(!string.IsNullOrEmpty(this.CountryCode) && !this.CountryCodeIsChecked) ||
				(!string.IsNullOrEmpty(this.OrgName) && !this.OrgNameIsChecked) ||
				(!string.IsNullOrEmpty(this.OrgDepartment) && !this.OrgDepartmentIsChecked) ||
				(!string.IsNullOrEmpty(this.OrgRole) && !this.OrgRoleIsChecked) ||
				(!string.IsNullOrEmpty(this.OrgNumber) && !this.OrgNumberIsChecked) ||
				(!string.IsNullOrEmpty(this.OrgAddress) && !this.OrgAddressIsChecked) ||
				(!string.IsNullOrEmpty(this.OrgAddress2) && !this.OrgAddress2IsChecked) ||
				(!string.IsNullOrEmpty(this.OrgZipCode) && !this.OrgZipCodeIsChecked) ||
				(!string.IsNullOrEmpty(this.OrgArea) && !this.OrgAreaIsChecked) ||
				(!string.IsNullOrEmpty(this.OrgCity) && !this.OrgCityIsChecked) ||
				(!string.IsNullOrEmpty(this.OrgRegion) && !this.OrgRegionIsChecked) ||
				(!string.IsNullOrEmpty(this.OrgCountryCode) && !this.OrgCountryCodeIsChecked))
			{
				await ServiceRef.UiSerializer.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.Incomplete)],
					ServiceRef.Localizer[nameof(AppResources.PleaseReviewAndCheckAllCheckboxes)]);
				return;
			}

			if (!this.CarefulReviewIsChecked)
			{
				await ServiceRef.UiSerializer.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.Incomplete)],
					ServiceRef.Localizer[nameof(AppResources.YouNeedToCheckCarefullyReviewed)]);
				return;
			}

			if (!this.ApprovePiiIsChecked)
			{
				await ServiceRef.UiSerializer.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.Incomplete)],
					ServiceRef.Localizer[nameof(AppResources.YouNeedToApproveToAssociate)]);
				return;
			}

			if (!await App.VerifyPin())
			{
				return;
			}

			(bool Succeeded1, byte[] Signature) = await ServiceRef.NetworkService.TryRequest(() => ServiceRef.XmppService.Sign(this.contentToSign, SignWith.LatestApprovedId));

			if (!Succeeded1)
			{
				return;
			}

			bool Succeeded2 = await ServiceRef.NetworkService.TryRequest(() =>
			{
				return ServiceRef.XmppService.SendPetitionSignatureResponse(
					this.signatoryIdentityId, this.contentToSign, Signature,
					this.petitionId, this.requestorFullJid, true);
			});

			if (Succeeded2)
			{
				await ServiceRef.NavigationService.GoBackAsync();
			}
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);
			await ServiceRef.UiSerializer.DisplayException(ex);
		}
	}

	[RelayCommand(CanExecute = nameof(CanExecuteCommands))]
	private async Task Reject()
	{
		if (this.requestorIdentity is null)
		{
			return;
		}

		try
		{
			bool Succeeded = await ServiceRef.NetworkService.TryRequest(() =>
			{
				return ServiceRef.XmppService.SendPetitionSignatureResponse(
					this.signatoryIdentityId, this.contentToSign, [],
					this.petitionId, this.requestorFullJid, false);
			});

			if (Succeeded)
			{
				await ServiceRef.NavigationService.GoBackAsync();
			}
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);
			await ServiceRef.UiSerializer.DisplayException(ex);
		}
	}

	[RelayCommand(CanExecute = nameof(CanExecuteCommands))]
	private async Task Revoke()
	{
		if (!this.IsPersonal)
		{
			return;
		}

		try
		{
			if (!await this.AreYouSure(ServiceRef.Localizer[nameof(AppResources.AreYouSureYouWantToRevokeYourLegalIdentity)]))
			{
				return;
			}

			(bool succeeded, LegalIdentity revokedIdentity) = await ServiceRef.NetworkService.TryRequest(() => ServiceRef.XmppService.ObsoleteLegalIdentity(this.LegalIdentity.Id));

			if (succeeded)
			{
				this.LegalIdentity = revokedIdentity;
				ServiceRef.TagProfile.RevokeLegalIdentity(revokedIdentity);

				await App.SetRegistrationPageAsync();
			}
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);
			await ServiceRef.UiSerializer.DisplayException(ex);
		}
	}

	private async Task<bool> AreYouSure(string Message)
	{
		if (!await App.VerifyPin())
		{
			return false;
		}

		return await ServiceRef.UiSerializer.DisplayAlert(
			ServiceRef.Localizer[nameof(AppResources.Confirm)], Message,
			ServiceRef.Localizer[nameof(AppResources.Yes)],
			ServiceRef.Localizer[nameof(AppResources.No)]);
	}

	[RelayCommand(CanExecute = nameof(CanExecuteCommands))]
	private async Task Compromise()
	{
		if (!this.IsPersonal)
		{
			return;
		}

		try
		{
			if (!await this.AreYouSure(ServiceRef.Localizer[nameof(AppResources.AreYouSureYouWantToReportYourLegalIdentityAsCompromized)]))
			{
				return;
			}

			(bool succeeded, LegalIdentity compromisedIdentity) = await ServiceRef.NetworkService.TryRequest(() => ServiceRef.XmppService.CompromiseLegalIdentity(this.LegalIdentity.Id));

			if (succeeded)
			{
				this.LegalIdentity = compromisedIdentity;
				ServiceRef.TagProfile.RevokeLegalIdentity(compromisedIdentity);

				await App.SetRegistrationPageAsync();
			}
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);
			await ServiceRef.UiSerializer.DisplayException(ex);
		}
	}

	[RelayCommand(CanExecute = nameof(CanExecuteCommands))]
	private async Task Transfer()
	{
		if (!this.IsPersonal)
		{
			return;
		}

		try
		{
			string Pin = await App.InputPin();

			if (Pin is null)
			{
				return;
			}

			if (!await ServiceRef.UiSerializer.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.Confirm)],
				ServiceRef.Localizer[nameof(AppResources.AreYouSureYouWantToTransferYourLegalIdentity)],
				ServiceRef.Localizer[nameof(AppResources.Yes)],
				ServiceRef.Localizer[nameof(AppResources.No)]))
			{
				return;
			}

			this.SetIsBusy(true);

			try
			{
				StringBuilder Xml = new();
				XmlWriterSettings Settings = XML.WriterSettings(false, true);

				using (XmlWriter Output = XmlWriter.Create(Xml, Settings))
				{
					Output.WriteStartElement("Transfer", ContractsClient.NamespaceOnboarding);

					await ServiceRef.XmppService.ExportSigningKeys(Output);

					Output.WriteStartElement("Pin");
					Output.WriteAttributeString("pin", Pin);
					Output.WriteEndElement();

					Output.WriteStartElement("Account", ContractsClient.NamespaceOnboarding);
					Output.WriteAttributeString("domain", ServiceRef.TagProfile.Domain);
					Output.WriteAttributeString("userName", ServiceRef.TagProfile.Account);
					Output.WriteAttributeString("password", ServiceRef.TagProfile.PasswordHash);

					if (!string.IsNullOrEmpty(ServiceRef.TagProfile.PasswordHashMethod))
					{
						Output.WriteAttributeString("passwordMethod", ServiceRef.TagProfile.PasswordHashMethod);
					}

					Output.WriteEndElement();
					Output.WriteEndElement();
				}

				using RandomNumberGenerator Rnd = RandomNumberGenerator.Create();
				byte[] Data = Encoding.UTF8.GetBytes(Xml.ToString());
				byte[] Key = new byte[16];
				byte[] IV = new byte[16];

				Rnd.GetBytes(Key);
				Rnd.GetBytes(IV);

				using Aes Aes = Aes.Create();
				Aes.BlockSize = 128;
				Aes.KeySize = 256;
				Aes.Mode = CipherMode.CBC;
				Aes.Padding = PaddingMode.PKCS7;

				using ICryptoTransform Transform = Aes.CreateEncryptor(Key, IV);
				byte[] Encrypted = Transform.TransformFinalBlock(Data, 0, Data.Length);

				Xml.Clear();

				using (XmlWriter Output = XmlWriter.Create(Xml, Settings))
				{
					Output.WriteStartElement("Info", ContractsClient.NamespaceOnboarding);
					Output.WriteAttributeString("base64", Convert.ToBase64String(Encrypted));
					Output.WriteAttributeString("once", "true");
					Output.WriteAttributeString("expires", XML.Encode(DateTime.UtcNow.AddMinutes(1)));
					Output.WriteEndElement();
				}

				XmlElement Response = await ServiceRef.XmppService.IqSetAsync(Constants.Domains.OnboardingDomain, Xml.ToString());

				foreach (XmlNode N in Response.ChildNodes)
				{
					if (N is XmlElement Info && Info.LocalName == "Code" && Info.NamespaceURI == ContractsClient.NamespaceOnboarding)
					{
						string Code = XML.Attribute(Info, "code");
						string Url = "obinfo:" + Constants.Domains.IdDomain + ":" + Code + ":" +
							Convert.ToBase64String(Key) + ":" + Convert.ToBase64String(IV);

						await ServiceRef.XmppService.AddTransferCode(Code);
						await ServiceRef.NavigationService.GoToAsync(nameof(TransferIdentityPage), new TransferIdentityNavigationArgs(Url));
						return;
					}
				}

				await ServiceRef.UiSerializer.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.UnexpectedResponse)]);
			}
			finally
			{
				this.SetIsBusy(false);
			}
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);
			await ServiceRef.UiSerializer.DisplayException(ex);
		}
	}

	//!!! needs is connected?
	[RelayCommand(CanExecute = nameof(CanExecuteCommands))]
	private Task ChangePin()
	{
		if (this.IsPersonal)
		{
			return SecurityViewModel.ChangePinAsync();
		}

		return Task.CompletedTask;
	}

	#region ILinkableView

	/// <summary>
	/// Title of the current view
	/// </summary>
	public override Task<string> Title => Task.FromResult<string>(ContactInfo.GetFriendlyName(this.LegalIdentity));

	#endregion


}
