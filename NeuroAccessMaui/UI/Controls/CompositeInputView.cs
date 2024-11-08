﻿using CommunityToolkit.Maui.Converters;
using Microsoft.Maui.Controls.Shapes;
using Path = Microsoft.Maui.Controls.Shapes.Path;
namespace NeuroAccessMaui.UI.Controls
{

	public partial class CompositeInputView : ContentView
	{

		// UI Elements
		protected Label label;
		protected ContentView leftContentView;
		protected ContentView centerContentView;
		protected ContentView rightContentView;
		protected Grid validationGrid;

		public CompositeInputView()
		{
			this.BuildContent();
		}

		private void BuildContent()
		{
			// Main Grid with 3 rows
			Grid mainGrid = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Auto }, // Label
					new RowDefinition { Height = GridLength.Auto }, // Left, Center, Right Views
					new RowDefinition { Height = GridLength.Auto }  // Validation Icon and Text
				}
			};

			// Row 1: Label
			this.label = new Label
			{
				FontAttributes = FontAttributes.Bold
			};
			this.label.SetBinding(Label.TextProperty, new Binding(nameof(this.LabelText), source: this));
			this.label.SetBinding(Label.StyleProperty, new Binding(nameof(this.LabelStyle), source: this));
			mainGrid.Add(this.label, 0, 0);

			// Row 2: LeftView, CenterView, RightView
			Grid contentGrid = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = GridLength.Auto }, // LeftView
					new ColumnDefinition { Width = GridLength.Star }, // CenterView
					new ColumnDefinition { Width = GridLength.Auto }  // RightView
				}
			};

			// LeftView
			this.leftContentView = new ContentView();
			this.leftContentView.SetBinding(ContentView.ContentProperty, new Binding(nameof(this.LeftView), source: this));
			contentGrid.Add(this.leftContentView, 0, 0);

			// CenterView
			this.centerContentView = new ContentView();
			this.centerContentView.SetBinding(ContentView.ContentProperty, new Binding(nameof(this.CenterView), source: this));
			contentGrid.Add(this.centerContentView, 1, 0);

			// RightView
			this.rightContentView = new ContentView();
			this.rightContentView.SetBinding(ContentView.ContentProperty, new Binding(nameof(this.RightView), source: this));
			contentGrid.Add(this.rightContentView, 2, 0);

			mainGrid.Add(contentGrid, 0, 1);

			// Row 3: ValidationIcon and ValidationText
			Grid validationGrid = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = GridLength.Auto }, // ValidationIcon
					new ColumnDefinition { Width = GridLength.Star }  // ValidationText
				}
			};

			// ValidationIcon
			Path validationIcon = new();
			validationIcon.SetBinding(Path.DataProperty, new Binding(nameof(this.ValidationIcon), source: this));
			validationIcon.SetBinding(IsVisibleProperty, new Binding(nameof(this.IsValid), source: this, converter: new InvertedBoolConverter()));
			validationGrid.Add(validationIcon, 0, 0);

			// ValidationText
			Label validationLabel = new Label();
			validationLabel.SetBinding(Label.TextProperty, new Binding(nameof(this.ValidationText), source: this));
			validationLabel.SetBinding(IsVisibleProperty, new Binding(nameof(this.IsValid), source: this, converter: new InvertedBoolConverter()));
			validationGrid.Add(validationLabel, 1, 0);

			mainGrid.Add(validationGrid, 0, 2);

			// Set the Content of the ContentView to the mainGrid
			this.Content = mainGrid;
		}


		#region Bindable Properties
		// LabelText Property
		public static readonly BindableProperty LabelTextProperty =
			 BindableProperty.Create(nameof(LabelText), typeof(string), typeof(CompositeInputView), string.Empty);

		public string LabelText
		{
			get => (string)this.GetValue(LabelTextProperty);
			set => this.SetValue(LabelTextProperty, value);
		}

		// LeftView Property
		public static readonly BindableProperty LeftViewProperty =
			 BindableProperty.Create(nameof(LeftView), typeof(View), typeof(CompositeInputView), null);

		public View LeftView
		{
			get => (View)this.GetValue(LeftViewProperty);
			set => this.SetValue(LeftViewProperty, value);
		}

		// CenterView Property
		public static readonly BindableProperty CenterViewProperty =
			 BindableProperty.Create(nameof(CenterView), typeof(View), typeof(CompositeInputView), null);

		public View CenterView
		{
			get => (View)this.GetValue(CenterViewProperty);
			set => this.SetValue(CenterViewProperty, value);
		}

		// RightView Property
		public static readonly BindableProperty RightViewProperty =
			 BindableProperty.Create(nameof(RightView), typeof(View), typeof(CompositeInputView), null);

		public View RightView
		{
			get => (View)this.GetValue(RightViewProperty);
			set => this.SetValue(RightViewProperty, value);
		}

		// ValidationIcon Property
		public static readonly BindableProperty ValidationIconProperty =
			 BindableProperty.Create(nameof(ValidationIcon), typeof(Geometry), typeof(CompositeInputView), null);

		public Geometry ValidationIcon
		{
			get => (Geometry)this.GetValue(ValidationIconProperty);
			set => this.SetValue(ValidationIconProperty, value);
		}

		// ValidationText Property
		public static readonly BindableProperty ValidationTextProperty =
			 BindableProperty.Create(nameof(ValidationText), typeof(string), typeof(CompositeInputView), string.Empty);

		public string ValidationText
		{
			get => (string)this.GetValue(ValidationTextProperty);
			set => this.SetValue(ValidationTextProperty, value);
		}

		/// <summary>
		/// Bindable property for the style applied to the validation label.
		/// </summary>
		public static readonly BindableProperty ValidationLabelStyleProperty =
			BindableProperty.Create(nameof(ValidationLabelStyle), typeof(Style), typeof(CompositeInputView));

		/// <summary>
		/// Gets or sets the style applied to the validation label.
		/// </summary>
		public Style ValidationIconStyle
		{
			get => (Style)this.GetValue(ValidationIconStyleProperty);
			set => this.SetValue(ValidationIconStyleProperty, value);
		}

		/// <summary>
		/// Bindable property for the style applied to the label above the entry.
		/// </summary>
		public static readonly BindableProperty ValidationIconStyleProperty =
			BindableProperty.Create(nameof(ValidationIconStyle), typeof(Style), typeof(CompositeInputView));

		/// <summary>
		/// Gets or sets the style applied to the label above the entry.
		/// </summary>
		public Style ValidationLabelStyle
		{
			get => (Style)this.GetValue(ValidationLabelStyleProperty);
			set => this.SetValue(ValidationLabelStyleProperty, value);
		}

		// IsValid Property
		public static readonly BindableProperty IsValidProperty =
			 BindableProperty.Create(nameof(IsValid), typeof(bool), typeof(CompositeInputView), true);

		public bool IsValid
		{
			get => (bool)this.GetValue(IsValidProperty);
			set => this.SetValue(IsValidProperty, value);
		}

		/// <summary>
		/// Bindable property for the style applied to the label above the entry.
		/// </summary>
		public static readonly BindableProperty LabelStyleProperty =
			BindableProperty.Create(nameof(LabelStyle), typeof(Style), typeof(CompositeInputView));

		/// <summary>
		/// Gets or sets the style applied to the label above the entry.
		/// </summary>
		public Style LabelStyle
		{
			get => (Style)this.GetValue(LabelStyleProperty);
			set => this.SetValue(LabelStyleProperty, value);
		}

		public static readonly BindableProperty ValidationColorProperty =
			BindableProperty.Create(nameof(ValidationColor), typeof(Color), typeof(CompositeInputView), Colors.Red);

		public Color ValidationColor
		{
			get => (Color)this.GetValue(ValidationColorProperty);
			set => this.SetValue(ValidationColorProperty, value);
		}
		#endregion



	}
}
