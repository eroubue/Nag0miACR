using Nag0mi.Gunbreaker.Control;

namespace Nag0mi.Tests;

internal static class WindowLeaseTests
{
    public static void Run()
    {
        VisibleOriginalIsReplacedAndRestored();
        HiddenOriginalRestoresItsCapturedVisibility();
        MissingOriginalStaysUnregistered();
        RepeatedActivationCapturesOriginalStateOnlyOnce();
        RepeatedDisposalDoesNotOverrideLaterHostChanges();
        SequentialLeasesCaptureFreshHostState();
        PartialRegistrationFailureRollsBack();
        RegistrationThatThrowsAfterAddingRollsBack();
        AlreadyRestoredRegistrationDoesNotDuplicateOriginal();
        DisposeBeforeActivationLeavesHostUntouched();
        ActivationAfterDisposalThrows();
        Console.WriteLine("PASS: window lease ownership, exact host restoration and registration rollback");
    }

    private static void VisibleOriginalIsReplacedAndRestored()
    {
        var fixture = new Fixture(originalOpen: true, originalRegistered: true);
        using var lease = fixture.CreateLease();

        lease.Activate();
        fixture.AssertActive();
        fixture.AssertQtUntouched();
        lease.Dispose();

        fixture.AssertRestored(originalOpen: true, originalRegistered: true);
        fixture.AssertQtUntouched();
    }

    private static void HiddenOriginalRestoresItsCapturedVisibility()
    {
        var fixture = new Fixture(originalOpen: false, originalRegistered: true);
        using var lease = fixture.CreateLease();
        lease.Activate();
        fixture.AssertActive();

        fixture.Original.IsOpen = true; // The host can still change the object while it is leased.
        lease.Dispose();

        fixture.AssertRestored(originalOpen: false, originalRegistered: true);
    }

    private static void MissingOriginalStaysUnregistered()
    {
        foreach (var originallyOpen in new[] { false, true })
        {
            var fixture = new Fixture(originallyOpen, originalRegistered: false);
            using var lease = fixture.CreateLease();
            lease.Activate();
            fixture.AssertActive();
            fixture.Original.IsOpen = !originallyOpen;

            lease.Dispose();

            fixture.AssertRestored(originallyOpen, originalRegistered: false);
        }
    }

    private static void RepeatedActivationCapturesOriginalStateOnlyOnce()
    {
        var fixture = new Fixture(originalOpen: false, originalRegistered: true);
        using var lease = fixture.CreateLease();
        lease.Activate();
        fixture.AssertActive();
        var mutations = fixture.Mutations.Count;
        fixture.Original.IsOpen = true;

        lease.Activate();

        Check.Equal(mutations, fixture.Mutations.Count);
        lease.Dispose();
        fixture.AssertRestored(originalOpen: false, originalRegistered: true);
    }

    private static void RepeatedDisposalDoesNotOverrideLaterHostChanges()
    {
        var fixture = new Fixture(originalOpen: true, originalRegistered: true);
        var lease = fixture.CreateLease();
        lease.Activate();
        lease.Dispose();
        fixture.AssertRestored(originalOpen: true, originalRegistered: true);
        var mutations = fixture.Mutations.Count;
        fixture.Original.IsOpen = false;

        lease.Dispose();

        Check.Equal(mutations, fixture.Mutations.Count);
        Check.True(!fixture.Original.IsOpen);
    }

    private static void SequentialLeasesCaptureFreshHostState()
    {
        var fixture = new Fixture(originalOpen: true, originalRegistered: true);
        using (var first = fixture.CreateLease())
        {
            first.Activate();
            fixture.AssertActive();
        }
        fixture.AssertRestored(originalOpen: true, originalRegistered: true);
        fixture.Original.IsOpen = false;
        using (var second = fixture.CreateLease())
        {
            second.Activate();
            fixture.AssertActive();
            fixture.Original.IsOpen = true;
        }

        fixture.AssertRestored(originalOpen: false, originalRegistered: true);
    }

    private static void PartialRegistrationFailureRollsBack()
    {
        var fixture = new Fixture(originalOpen: true, originalRegistered: true);
        fixture.FailOnAdd = fixture.Settings;
        using var lease = fixture.CreateLease();

        var error = CaptureException(lease.Activate);

        Check.True(ReferenceEquals(fixture.AddFailure, error), "Keep the original registration failure.");
        fixture.AssertRestored(originalOpen: true, originalRegistered: true);
        fixture.AssertQtUntouched();
    }

    private static void RegistrationThatThrowsAfterAddingRollsBack()
    {
        var fixture = new Fixture(originalOpen: false, originalRegistered: true);
        fixture.FailOnAdd = fixture.Settings;
        fixture.FailAfterAdding = true;
        using var lease = fixture.CreateLease();

        var error = CaptureException(lease.Activate);

        Check.True(ReferenceEquals(fixture.AddFailure, error));
        fixture.AssertRestored(originalOpen: false, originalRegistered: true);
        fixture.AssertQtUntouched();
    }

    private static void AlreadyRestoredRegistrationDoesNotDuplicateOriginal()
    {
        var fixture = new Fixture(originalOpen: true, originalRegistered: true);
        using var lease = fixture.CreateLease();
        lease.Activate();
        fixture.AssertActive();
        fixture.Registered.Add(fixture.Original);
        fixture.Registered.Remove(fixture.Bar);

        lease.Dispose();

        fixture.AssertRestored(originalOpen: true, originalRegistered: true);
    }

    private static void DisposeBeforeActivationLeavesHostUntouched()
    {
        var fixture = new Fixture(originalOpen: true, originalRegistered: true);
        var lease = fixture.CreateLease();

        lease.Dispose();
        lease.Dispose();

        Check.Equal(0, fixture.Mutations.Count);
        Check.True(fixture.Registered.Contains(fixture.Original));
        Check.True(fixture.Original.IsOpen);
        fixture.AssertQtUntouched();
        Check.True(CaptureException(lease.Activate) is ObjectDisposedException);
    }

    private static void ActivationAfterDisposalThrows()
    {
        var fixture = new Fixture(originalOpen: true, originalRegistered: true);
        var lease = fixture.CreateLease();
        lease.Activate();
        lease.Dispose();

        Check.True(CaptureException(lease.Activate) is ObjectDisposedException);
        fixture.AssertRestored(originalOpen: true, originalRegistered: true);
    }

    private static Exception? CaptureException(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception error)
        {
            return error;
        }
    }

    private sealed class FakeWindow(string name, bool isOpen)
    {
        public string Name { get; } = name;
        public bool IsOpen { get; set; } = isOpen;
    }

    private sealed class Fixture
    {
        public FakeWindow Original { get; }
        public FakeWindow Bar { get; } = new("bar", true);
        public FakeWindow Settings { get; } = new("settings", true);
        public FakeWindow Qt { get; } = new("qt", true);
        public HashSet<FakeWindow> Registered { get; } = [];
        public List<FakeWindow> Mutations { get; } = [];
        public FakeWindow? FailOnAdd { get; set; }
        public bool FailAfterAdding { get; set; }
        public Exception AddFailure { get; } = new InvalidOperationException("Injected registration failure");

        public Fixture(bool originalOpen, bool originalRegistered)
        {
            Original = new FakeWindow("original", originalOpen);
            if (originalRegistered) Registered.Add(Original);
            Registered.Add(Qt);
        }

        public WindowLease<FakeWindow> CreateLease() => new(Original, [Bar, Settings],
            Registered.Contains, Add, Remove, window => window.IsOpen, (window, open) =>
            {
                Mutations.Add(window);
                window.IsOpen = open;
            });

        public void AssertActive()
        {
            Check.True(!Registered.Contains(Original));
            Check.True(!Original.IsOpen);
            Check.True(Registered.Contains(Bar));
            Check.True(Registered.Contains(Settings));
        }

        public void AssertRestored(bool originalOpen, bool originalRegistered)
        {
            Check.Equal(originalRegistered, Registered.Contains(Original));
            Check.Equal(originalOpen, Original.IsOpen);
            Check.True(!Registered.Contains(Bar));
            Check.True(!Registered.Contains(Settings));
            Check.True(!Bar.IsOpen);
            Check.True(!Settings.IsOpen);
        }

        public void AssertQtUntouched()
        {
            Check.True(Registered.Contains(Qt));
            Check.True(Qt.IsOpen);
            Check.True(!Mutations.Contains(Qt));
        }

        private void Add(FakeWindow window)
        {
            Mutations.Add(window);
            if (ReferenceEquals(window, FailOnAdd) && !FailAfterAdding) throw AddFailure;
            if (!Registered.Add(window)) throw new InvalidOperationException($"Duplicate {window.Name}");
            if (ReferenceEquals(window, FailOnAdd)) throw AddFailure;
        }

        private void Remove(FakeWindow window)
        {
            Mutations.Add(window);
            if (!Registered.Remove(window)) throw new InvalidOperationException($"Missing {window.Name}");
        }
    }
}
