﻿namespace NeuroAccessMaui.Services.Navigation
{
	/// <summary>
	/// An base class holding page specific navigation parameters.
	/// </summary>
	public class NavigationArgs
	{
		private NavigationArgs? parentArgs = null;
		private BackMethod backMethod = BackMethod.Inherited;
		private string? uniqueId = null;

		/// <summary>
		/// Sets the reference to the main parent's <see cref="NavigationArgs"/>.
		/// </summary>
		public void SetBackArguments(NavigationArgs? ParentArgs, BackMethod BackMethod = BackMethod.Inherited, string? UniqueId = null)
		{
			this.backMethod = BackMethod;
			this.parentArgs = ParentArgs;
			this.uniqueId = UniqueId;
		}

		/// <summary>
		/// Get the route used for the <see cref="INavigationService.GoBackAsync"/> method.
		/// </summary>
		public string GetBackRoute()
		{
			BackMethod BackMethod = this.backMethod;

			if (BackMethod == BackMethod.Inherited)
			{
				string BackRoute = "..";
				NavigationArgs? ParentArgs = this.parentArgs;

				while ((ParentArgs is not null) && (ParentArgs.backMethod == BackMethod.Inherited))
				{
					ParentArgs = ParentArgs.parentArgs;
					BackRoute += "/..";
				}

				if (ParentArgs is null)
					return ".."; // Pop is inherited by default

				BackMethod ParentBackMethod = ParentArgs.backMethod;

				if (ParentBackMethod == BackMethod.Pop)
					return "..";
				else if (ParentBackMethod == BackMethod.ToThisPage)
					return BackRoute + "/..";
				/*
				else if (ParentBackMethod == BackMethod.ToParentPage)
					return BackRoute + "/../..";
				else if (ParentBackMethod == BackMethod.ToMainPage)
					return "///" + nameof(MainPage);
				*/
			}
			else
			{
				if (BackMethod == BackMethod.Pop)
					return "..";
				else if (BackMethod == BackMethod.ToThisPage)
					return "..";
				/*
				else if (BackMethod == BackMethod.ToParentPage)
					return "../..";
				if (this.backMethod == BackMethod.ToMainPage)
					return "///" + nameof(MainPage);
				*/
			}

			// all variants should be returned by now
			throw new NotImplementedException();
		}

		/// <summary>
		/// An unique view identifier used to search the args of similar view types.
		/// </summary>
		public string? GetUniqueId() => this.uniqueId;

		/// <summary>
		/// Is the navigation animated
		/// </summary>
		public bool Animated { get; set; } = true;
	}
}
