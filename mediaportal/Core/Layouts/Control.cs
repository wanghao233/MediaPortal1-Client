using System;
using System.Drawing;

namespace MediaPortal.Layouts
{
	public abstract class Control : ILayoutComponent
	{
		#region Methods

		void ILayoutComponent.Arrange(Rectangle rect)
		{
			ArrangeCore(rect);
		}

		protected virtual void ArrangeCore(Rectangle rect)
		{
		}

		Size ILayoutComponent.Measure()
		{
			return MeasureCore();
		}

		protected virtual Size MeasureCore()
		{
			return Size.Empty;
		}

		#endregion Methods

		#region Properties

		Dock ILayoutComponent.Dock
		{
			get { return _dock; }
			set { _dock = value; }
		}

		public abstract Size Size
		{
			get;
			set;
		}

		public abstract Point Location
		{
			get;
			set;
		}

		public abstract bool Visible
		{
			get;
			set;
		}

		#endregion Properties

		#region Fields

		Dock						_dock = Dock.None;

//		protected Size				_size = Size.Empty;
//		protected Point				_location = Point.Empty;

		#endregion Fields
	}
}
