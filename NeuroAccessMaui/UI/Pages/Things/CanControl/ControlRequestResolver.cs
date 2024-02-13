﻿using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.Notification.Things;
using System.Globalization;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Provisioning;

namespace NeuroAccessMaui.UI.Pages.Things.CanControl
{
	/// <summary>
	/// Resolves pending control requests
	/// </summary>
	public class ControlRequestResolver : IEventResolver
	{
		private readonly string bareJid;
		private readonly string remoteJid;
		private readonly RuleRange range;
		private readonly ProvisioningToken? token;

		/// <summary>
		/// Resolves pending control requests
		/// </summary>
		public ControlRequestResolver(string BareJid, string RemoteJid, RuleRange Range)
		{
			this.bareJid = BareJid.ToLower(CultureInfo.InvariantCulture);
			this.remoteJid = RemoteJid.ToLower(CultureInfo.InvariantCulture);
			this.range = Range;
			this.token = null;
		}

		/// <summary>
		/// Resolves pending control requests
		/// </summary>
		public ControlRequestResolver(string BareJid, string RemoteJid, ProvisioningToken Token)
		{
			this.bareJid = BareJid.ToLower(CultureInfo.InvariantCulture);
			this.remoteJid = RemoteJid.ToLower(CultureInfo.InvariantCulture);
			this.range = default;
			this.token = Token;
		}

		/// <summary>
		/// If the resolver resolves an event.
		/// </summary>
		/// <param name="Event">Pending notification event.</param>
		/// <returns>If the resolver resolves the event.</returns>
		public bool Resolves(NotificationEvent Event)
		{
			if (Event.Type != NotificationEventType.Things || Event is not CanControlNotificationEvent CanControlNotificationEvent)
				return false;

			if (CanControlNotificationEvent.BareJid != this.bareJid)
				return false;

			if (this.token is null)
			{
				return this.range switch
				{
					RuleRange.All => true,
					RuleRange.Domain => string.Equals(XmppClient.GetDomain(this.remoteJid), XmppClient.GetDomain(CanControlNotificationEvent.RemoteJid), StringComparison.OrdinalIgnoreCase),
					RuleRange.Caller => string.Equals(this.remoteJid, CanControlNotificationEvent.RemoteJid, StringComparison.OrdinalIgnoreCase),
					_ => false,
				};
			}
			else
			{
				return this.token.Type switch
				{
					TokenType.User => this.IsResolved(CanControlNotificationEvent.UserTokens),
					TokenType.Service => this.IsResolved(CanControlNotificationEvent.ServiceTokens),
					TokenType.Device => this.IsResolved(CanControlNotificationEvent.DeviceTokens),
					_ => false,
				};
			}
		}

		private bool IsResolved(ProvisioningToken[]? Tokens)
		{
			if (Tokens is null)
				return false;

			foreach (ProvisioningToken Token in Tokens)
			{
				if (Token.Token == this.token?.Token)
					return true;
			}

			return false;
		}

	}
}
