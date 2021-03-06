using System;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.Drawing;

namespace MonoTouch.Dialog
{
	/// <summary>
	/// An element that can be used to enter text.
	/// </summary>
	/// <remarks>
	/// This element can be used to enter text both regular and password protected entries. 
	///     
	/// The Text fields in a given section are aligned with each other.
	/// </remarks>
	public class MultilineEntryElement : EntryElement, IElementSizing
	{
		public string PlaceholderText {get;set;}
		/// <summary>
		/// The key used for reusable UITableViewCells.
		/// </summary>
		static NSString entryKey = new NSString ("MultilineEntryElement");

		protected virtual NSString EntryKey {
			get {
				return entryKey;
			}
		}
		
		public UITextAutocapitalizationType AutocapitalizationType {
			get {
				return autocapitalizationType;	
			}
			set { 
				autocapitalizationType = value;
				if (entry != null)
					entry.AutocapitalizationType = value;
			}
		}
		
		public UITextAutocorrectionType AutocorrectionType { 
			get { 
				return autocorrectionType;
			}
			set { 
				autocorrectionType = value;
				if (entry != null)
					this.autocorrectionType = value;
			}
		}
		
		private float height = 112;
		public float Height {
			get {
				return height;
			}
			set {
				height = value;
			}
		}
		
		private UIFont inputFont = UIFont.SystemFontOfSize(17);
		public UIFont Font {
			get {
				return inputFont;
			}
			set {
				inputFont = value;
				if (entry != null)
					entry.Font = value;
			}
		}

		UITextAutocapitalizationType autocapitalizationType = UITextAutocapitalizationType.Sentences;
		UITextAutocorrectionType autocorrectionType = UITextAutocorrectionType.Default;
		bool becomeResponder;
		DialogTextView entry;
		static UIFont font = UIFont.BoldSystemFontOfSize (17);

		public event EventHandler Changed;
		public event Func<bool> ShouldReturn;
		/// <summary>
		/// Constructs an MultilineEntryElement with the given caption, placeholder and initial value.
		/// </summary>
		/// <param name="caption">
		/// The caption to use
		/// </param>
		/// <param name="value">
		/// Initial value.
		/// </param>
		public MultilineEntryElement (string caption, string value) : base (caption, "", "")
		{ 
			Value = value;
		}

		public override string Summary ()
		{
			return Value;
		}

		// 
		// Computes the X position for the entry by aligning all the entries in the Section
		//
		SizeF ComputeEntryPosition (UITableView tv, UITableViewCell cell)
		{
			Section s = Parent as Section;
			if (s.EntryAlignment.Width != 0)
				return s.EntryAlignment;
			
			// If all EntryElements have a null Caption, align UITextField with the Caption
			// offset of normal cells (at 10px).
			SizeF max = new SizeF (-15, tv.StringSize ("M", font).Height);
			foreach (var e in s.Elements) {
				var ee = e as EntryElement;
				if (ee == null)
					continue;
				
				if (ee.Caption != null) {
					var size = tv.StringSize (ee.Caption, font);
					if (size.Width > max.Width)
						max = size;
				}
			}
			s.EntryAlignment = new SizeF (25 + Math.Min (max.Width, 160), max.Height);
			return s.EntryAlignment;
		}

		protected virtual DialogTextView CreateTextField (RectangleF frame)
		{
			return new DialogTextView (frame) {
				AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleLeftMargin,
				Text = Value ?? "",
				Tag = 1,
				BackgroundColor = UIColor.Clear
			};
		}
		
		static NSString cellkey = new NSString ("MultilineEntryElement");

		UITableView _tableView;

		
		public override UITableViewCell GetCell (UITableView tv)
		{
			_tableView = tv;
			var cell = tv.DequeueReusableCell (cellkey);

			if (cell == null) {
				cell = new UITableViewCell (UITableViewCellStyle.Default, cellkey);
				cell.SelectionStyle = UITableViewCellSelectionStyle.None;
			} else {
				//RemoveTag (cell, 1);
			}
			cell.BackgroundColor = Appearance.BackgroundColorEditable;

			if (entry == null) {
				SizeF size = ComputeEntryPosition (tv, cell);
				float width = cell.ContentView.Bounds.Width;
				
				entry = CreateTextField (new RectangleF (	0, 0, width, size.Height + (height)));
				entry.Font = Appearance.LabelFont;
				
				var toolbar =  new UIToolbar();
				toolbar.Items = new UIBarButtonItem[] {
				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
				new UIBarButtonItem("Done", UIBarButtonItemStyle.Done, (e, a)=>{
						entry.ResignFirstResponder();
					})
				};
				toolbar.SizeToFit();
				entry.InputAccessoryView = toolbar;		

				entry.Font = inputFont;
				
				entry.Changed += delegate {
					FetchValue ();
				};
				entry.Ended += delegate {
					FetchValue ();
					if (OnValueChanged!=null) OnValueChanged(this);
				};
//				entry.ShouldReturn += delegate {
//					
//					if (ShouldReturn != null)
//						return ShouldReturn ();
//					
//					RootElement root = GetImmediateRootElement ();
//					EntryElement focus = null;
//					
//					if (root == null)
//						return true;
//					
//					foreach (var s in root.Sections) {
//						foreach (var e in s.Elements) {
//							if (e == this) {
//								focus = this;
//							} else if (focus != null && e is EntryElement) {
//								focus = e as EntryElement;
//								break;
//							}
//						}
//						
//						if (focus != null && focus != this)
//							break;
//					}
//					
//					if (focus != this)
//						focus.BecomeFirstResponder (true);
//					else 
//						focus.ResignFirstResponder (true);
//					
//					return true;
//				};
				entry.Started += delegate {
					entry.ReturnKeyType = UIReturnKeyType.Default;
				};
			}
			if (becomeResponder) {
				entry.BecomeFirstResponder ();
				becomeResponder = false;
			}
			entry.KeyboardType = KeyboardType;
			entry.PlaceholderText = PlaceholderText;

			entry.AutocapitalizationType = AutocapitalizationType;
			entry.AutocorrectionType = AutocorrectionType;
			
			cell.TextLabel.Text = Caption;

			cell.ContentView.AddSubview (entry);
			return cell;
		}
		
		/// <summary>
		///  Copies the value from the UITextField in the EntryElement to the
		//   Value property and raises the Changed event if necessary.
		/// </summary>
		public void FetchValue ()
		{
			if (entry == null)
				return;

			var newValue = entry.Text;
			if (newValue == Value)
				return;
			
			Value = newValue;
			
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}
		
		protected override void Dispose (bool disposing)
		{
			_tableView = null;
			if (disposing) {
				if (entry != null) {
					entry.Dispose ();
					entry = null;
				}
			}
		}

		public override void Selected (DialogViewController dvc, UITableView tableView, NSIndexPath indexPath)
		{
			BecomeFirstResponder (true);
			tableView.DeselectRow (indexPath, true);
		}
		
		public override bool Matches (string text)
		{
			return (Value != null ? Value.IndexOf (text, StringComparison.CurrentCultureIgnoreCase) != -1 : false) || base.Matches (text);
		}
		
		/// <summary>
		/// Makes this cell the first responder (get the focus)
		/// </summary>
		/// <param name="animated">
		/// Whether scrolling to the location of this cell should be animated
		/// </param>
		public void BecomeFirstResponder (bool animated)
		{
			becomeResponder = true;
			if (_tableView == null)
				return;
			_tableView.ScrollToRow (this.GetIndexPath(), UITableViewScrollPosition.Middle, animated);
			if (entry != null) {
				entry.BecomeFirstResponder ();
				becomeResponder = false;
			}
		}

		public void ResignFirstResponder (bool animated)
		{
			becomeResponder = false;
			if (_tableView == null)
				return;
			_tableView.ScrollToRow (GetIndexPath(), UITableViewScrollPosition.Middle, animated);
			if (entry != null)
				entry.ResignFirstResponder ();
				
		}

		#region IElementSizing implementation
		public float GetHeight (MonoTouch.UIKit.UITableView tableView, MonoTouch.Foundation.NSIndexPath indexPath)
		{
			return height;
		}
		#endregion
	}
}
