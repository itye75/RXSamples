using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using NUnit.Framework;
using Serilog;


namespace RXSamples
{
    [TestFixture]
    public class Samples
    {
        #region Public Methods

        /*
        [SetUp]
        public void Setup()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                // .WriteTo.File("log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }
        */

        [TestCase]
        public void A01_Range()
        {
            var o = Observable.Range(1, 10);
            //Task.Delay(TimeSpan.FromSeconds(2)).Wait();
            o.Subscribe(num => TestContext.Progress.WriteLine(num));
        }
        
        [TestCase]
        public void A02_Interval()
        {
            var o = Observable.Interval(TimeSpan.FromSeconds(0.5)).Take(20);
            o.Subscribe(num => TestContext.Progress.WriteLine(num));
            Task.Delay(TimeSpan.FromSeconds(10)).Wait();
        }

        [TestCase]
        public void A03_Generate()
        {
            var o = Observable.Generate(0,
               i => i < 10,
               i => i + 1,
               i => i * i);

            o.Subscribe(num => TestContext.Progress.WriteLine(num));
        }

        [TestCase]
        public void A04_GenerateWithTime()
        {
            TimeSpan iterationTime = TimeSpan.FromMilliseconds(1000);

            var o = Observable.Generate(0,
               i => i < 10,
               i => i + 1,
               i => i * i,
               i => iterationTime);

            TimeSpan delayTime = TimeSpan.FromSeconds(3);
            var o2 = o.Delay(delayTime);

            o.Subscribe(num => TestContext.Progress.WriteLine($"o:{num}, {DateTime.Now.Second}"));
            o2.Subscribe(num => TestContext.Progress.WriteLine($"o2:{num}, {DateTime.Now.Second}"));
            
            Task.Delay(delayTime+TimeSpan.FromSeconds(4)).Wait();
        }

        [TestCase]
        public void A05_repeat()
        {
            var o1 = Observable.Range(1, 5);
            var o2 = o1.Repeat(5);

            o1.Subscribe(n => TestContext.Progress.Write($"{n};"));
            TestContext.Progress.WriteLine();
            o2.Subscribe(n => TestContext.Progress.Write($"{n};"));
        }

        [TestCase]
        public void A06_ToObservable()
        {
            int[] array = { 1, 3, 5, 7 };
            IObservable<int> o = array.ToObservable();
            var disposable = o.Subscribe(v => TestContext.Progress.Write(v + "; ")); // 1; 3; 5; 7; 
            disposable.Dispose();
        }

        [TestCase]
        public void A07_Disposable()
        {
            IDisposable disposable = Disposable.Create(() => TestContext.Progress.WriteLine("disposed!"));
            disposable.Dispose();
        }
        
        [TestCase]
        public void A08_ToObservable()
        {
            var foldersObservable = Directory.GetDirectories(@"c:\").ToObservable();
            // var folderObservable = Observable.ToObservable(Directory.GetDirectories(@"c:\"));
            foldersObservable.Subscribe(folder => TestContext.Progress.WriteLine(folder));
            Task.Delay(TimeSpan.FromSeconds(5));
        }
        
        [TestCase]
        public void A09_ToObservableAsync()
        {
            var foldersObservableTask = Task.Run(() => Directory.GetDirectories(@"c:\")).ToObservable();    // raises single event of end of task

            // subscription run on the genuine thread
            foldersObservableTask.Subscribe(folder => folder.ToList().ForEach(folderName => TestContext.Progress.WriteLine(folderName)));

            Task.Delay(TimeSpan.FromSeconds(5));
        }

        
        [TestCase]
        public void A10_Subject()
        {
            ISubject<int> subject = new Subject<int>();
            IObservable<int> observable = subject;

            Action<int> onNextHandler = num => TestContext.Progress.WriteLine(num);
            Action<Exception> onErrorHandler = e => TestContext.Progress.WriteLine(e);
            Action onCompleted = () => TestContext.Progress.WriteLine("Finished");

            observable.Subscribe(onNextHandler, onErrorHandler, onCompleted);

            subject.OnNext(10);
            subject.OnNext(20);
            subject.OnNext(30);

            subject.OnError(new InvalidDataException("Bad number"));

            // another registration will raise the same InvalidDataException because that is what on the subject
            // observable.Subscribe(onNextHandler, onErrorHandler, onCompleted);

            // OnError & OnCompleted disposes the Subject stream!
            subject.OnNext(40);     // won't raise!!

            // in order to demo complete we must create a new subject
            subject = new Subject<int>();
            observable = subject;
            observable.Subscribe(onNextHandler, onErrorHandler, onCompleted);
            subject.OnCompleted();
        }

        [TestCase]
        public void A11_Subscribed_ShouldOnError()
        {
            ISubject<int> subject = new Subject<int>();
            IObservable<int> observable = subject;

            Task.Run(() =>
            {
                Action<int> onNext = num => TestContext.Progress.WriteLine(num);

                observable.Subscribe(onNext);

                subject.OnNext(10);
                subject.OnNext(20);
                subject.OnNext(30);

                // onError was not registered and so Task exception wasn't shown!
                subject.OnError(new InvalidDataException("Bad number"));
            });
        }

        [TestCase]
        public void A12_SynchroniseSubject()
        {
            TestContext.Progress.WriteLine($"Method threadId = {Thread.CurrentThread.ManagedThreadId}");

            ISubject<int> subject = new Subject<int>();
            var eventLoopScheduler = new EventLoopScheduler();

            // -----> change remarks <------
            ISubject<int, int> syncedSubject = Subject.Synchronize(subject, eventLoopScheduler);
            //			var syncedSubject = Subject.Synchronize(subject, Scheduler.Immediate);	        // immediately on the current thread
            //			var syncedSubject = Subject.Synchronize(subject, Scheduler.Default);	        // on the platform's default scheduler
            //			var syncedSubject = Subject.Synchronize(subject, Scheduler.CurrentThread);	    // as soon as possible on the current thread


            syncedSubject.Subscribe(num => TestContext.Progress.WriteLine($"received thread:{Thread.CurrentThread.ManagedThreadId}"));

            subject.Subscribe(num => TestContext.Progress.WriteLine($"received thread:{Thread.CurrentThread.ManagedThreadId}"));

            TestContext.Progress.WriteLine($"send thread:{Thread.CurrentThread.ManagedThreadId}");
            subject.OnNext(0);

            Task.Delay(TimeSpan.FromSeconds(1)).Wait();
        }

        [TestCase]
        public void A13_Do()
        {
            IList<int> numbers = new List<int> { 1, 4, 7 };
            IObservable<int> numbersObservable = numbers.ToObservable();

            numbersObservable
                .Do(number => TestContext.Progress.WriteLine($"do1={number}"))
                .Select(number => number * 2)
                .Do(number => TestContext.Progress.WriteLine($"do2={number}"))
                .Subscribe(number => TestContext.Progress.WriteLine($"final subscribe:{number}"));
        }

        [TestCase]
        public void A14_Select_Where()
        {
            var o = Observable.Generate(0,
                i => i < 10,
                i => i + 1,
                i => -i,
                i => TimeSpan.FromSeconds(0.1));    // -1,-2...

            o.Where(number => number % 2 == 0)
                .Select(number => number * 2)
                .Subscribe(number => TestContext.Progress.WriteLine(number));

            Task.Delay(TimeSpan.FromSeconds(1)).Wait();
        }

        [TestCase]
        public void A15_DistinctUntilChanged()
        {
            ISubject<KeyValuePair<int, string>> values = new Subject<KeyValuePair<int, string>>();
            values.DistinctUntilChanged().Subscribe(pair => TestContext.Progress.WriteLine($"any change: {pair}"));
            values.DistinctUntilChanged(pair => pair.Value).Subscribe(pair => TestContext.Progress.WriteLine($"second field changed: {pair}"));

            values.OnNext(new KeyValuePair<int, string>(1, "A"));
            values.OnNext(new KeyValuePair<int, string>(1, "A"));
            values.OnNext(new KeyValuePair<int, string>(1, "A"));
            values.OnNext(new KeyValuePair<int, string>(2, "A"));
            values.OnNext(new KeyValuePair<int, string>(2, "A"));
            values.OnNext(new KeyValuePair<int, string>(2, "A"));
            values.OnNext(new KeyValuePair<int, string>(1, "B"));
            values.OnNext(new KeyValuePair<int, string>(1, "B"));
            values.OnNext(new KeyValuePair<int, string>(1, "B"));
        }

        [TestCase]
        public void A16A_BehaviorSubject()
        {
            var behaviorSubject = new BehaviorSubject<int>(2);
            
            IObservable<int> observable = behaviorSubject;

            observable.Subscribe(num => TestContext.Progress.WriteLine($"handler:{num}"));

            TestContext.Progress.WriteLine($"Subject.Value{behaviorSubject.Value}");

            behaviorSubject.OnNext(5);
            TestContext.Progress.WriteLine($"Subject.Value{behaviorSubject.Value}");
        }

        [TestCase]
        public void A16B_ReplaySubject()
        {
            var replaySubject = new ReplaySubject<int>(3);

            IObservable<int> observable = replaySubject;

            replaySubject.OnNext(1);
            replaySubject.OnNext(2);
            replaySubject.OnNext(3);
            replaySubject.OnNext(4);

            observable.Subscribe(num => TestContext.Progress.WriteLine($"{num}"));      // 2,3,4 will be shown
        }

        [TestCase]
        public void A17_EventLoopScheduler()
        {
            TestContext.Progress.WriteLine($"Method threadId = {Thread.CurrentThread.ManagedThreadId}");

            var e = new EventLoopScheduler();
            ISubject<Unit> subject = new Subject<Unit>();

            subject.Subscribe(i => TestContext.Progress.WriteLine($"1: {Thread.CurrentThread.ManagedThreadId}")); // on default thread
            IObservable<Unit> observable = subject.ObserveOn(e);
            //			IObservable<Unit> observable2 = subject.ObserveOn(e);
            observable.Subscribe(i => TestContext.Progress.WriteLine($"2: {Thread.CurrentThread.ManagedThreadId}")); // on e thread

            subject.OnNext(Unit.Default);

            Thread.Sleep(500);
        }

        [TestCase]
        public void A18_EventLoopScheduler()
        {
            TestContext.Progress.WriteLine($"Main Thread={Thread.CurrentThread.ManagedThreadId}");

            ISubject<int> subject = new Subject<int>();
            var eventLoopSchedulerSource = new EventLoopScheduler();
            var eventLoopSchedulerDest = new EventLoopScheduler();

            subject.ObserveOn(eventLoopSchedulerDest).Subscribe(
                sourceThreadId => TestContext.Progress.WriteLine($"source thread={sourceThreadId}, current thread={Thread.CurrentThread.ManagedThreadId}"));

            object state = new object();
            eventLoopSchedulerSource.SchedulePeriodic(state, TimeSpan.FromSeconds(1), a => subject.OnNext(Thread.CurrentThread.ManagedThreadId));

            //eventLoopSchedulerSource.Schedule<object>(null, TimeSpan.FromSeconds(1), (p_scheduler, p_action) => subject.OnNext(Thread.CurrentThread.ManagedThreadId));

            Thread.Sleep(5000);
        }

        [TestCase]
        public void A19_Merge()
        {
            var o1 = Observable.Generate(0,
               i => i < 50,
               i => i + 1,
               i => i + 1,
               i => TimeSpan.FromMilliseconds(100));

            var o2 = Observable.Generate(100,
            i => i < 110,
            i => i + 1,
            i => i + 1,
            i => TimeSpan.FromMilliseconds(500));

            var o3 = o1.Merge(o2);
            o3.Subscribe(num => TestContext.Progress.WriteLine(num));

            Task.Delay(TimeSpan.FromSeconds(5)).Wait();
        }

        [TestCase]
        public void A20_FromEventPattern()
        {
            ObservableCollection<int> oc = new ObservableCollection<int>();
            
            //oc.CollectionChanged += Oc_CollectionChanged;
            var ocChanged =
                Observable.FromEventPattern<System.Collections.Specialized.NotifyCollectionChangedEventArgs>(oc,
                    nameof(oc.CollectionChanged));
            
            ocChanged.Subscribe(eventArgs => TestContext.Progress.WriteLine(eventArgs?.EventArgs.NewItems?[0]));
            oc.Add(5);

            /*
            // on ui application
            var mouseMoves = Observable.FromEventPattern<MouseEventArgs>(frm, nameof(frm.MouseMove));
			mouseMoves.Subscribe(evt => { lbl.Text = evt.EventArgs.Location.ToString(); }))			
            */

            /////////////////////////////////////////////////////////////////////////////////////////////////////
            // use FromEventPattern when event is of type EventHanlder, use FromEvent when using custom event
            /////////////////////////////////////////////////////////////////////////////////////////////////////
            
            /*
            // use o.ObserveOnDispatcher().Subscribe(handler) in WPF thread to return to Dispatcher thread
            */
        }

        [TestCase]
        public void A21_FromAsync()
        {
            Task<int> CalcFileLength() => Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(t => 1024 /*calc length*/);
            var readLengthObservable = Observable.FromAsync(CalcFileLength);
            readLengthObservable.Subscribe(bytesRead => TestContext.Progress.WriteLine(bytesRead));
            readLengthObservable.Subscribe(bytesRead => TestContext.Progress.WriteLine(bytesRead)); // each subscribe will trigger the Task
            Task.Delay(TimeSpan.FromSeconds(3)).Wait();
        }
        
        [TestCase]
        public void A22_ReadFileAsync()
        {
            Stream stream = new FileStream(@"C:\build.ini", FileMode.Open);
            var o = Observable.Using(
                () => new StreamReader(stream),
                reader => Observable.FromAsync(reader.ReadLineAsync).Repeat().TakeWhile(line => line != null));
            o.Subscribe(line => TestContext.Progress.WriteLine(line));  // run on the genuine thread
        }

        [TestCase]
        public void A23_OnErrorHandling()
        {
            AutoResetEvent autoResetEvent = new AutoResetEvent(false);

            var observable = Observable.Generate(
                0,
                i => i < 3,
                i => i + 1,
                i => i + 1,
                t => TimeSpan.FromMilliseconds(1000));

            observable.Subscribe(num => TestContext.Progress.WriteLine($"no timeout subscription: num={num}"));

            // Timeout - Applies a timeout policy for each element in the observable sequence. If the next element isn't received within the specified timeout duration starting from its predecessor, a TimeoutException is propagated to the observer
            observable.Timeout(TimeSpan.FromMilliseconds(100)).Subscribe(
                num => TestContext.Progress.WriteLine($"received before timeout: num={num}"), 
                ex => TestContext.Progress.WriteLine($"wasn't received before timeout: {ex}"));      // onerror handling

            observable.Where(i => i == 3).Subscribe(n => autoResetEvent.Set());
            autoResetEvent.WaitOne();
        }

        [TestCase]
        public async Task A24_AsyncAwait()
        {
            var xs = Observable.Range(0, 10, ThreadPoolScheduler.Instance);
            Console.WriteLine("Last = " + await xs);                    // 9
            Console.WriteLine("First = " + await xs.FirstAsync());      // 0
            Console.WriteLine("Third = " + await xs.ElementAt(3));      // 3
        }

        [TestCase]
        public void A30_Buffer()
        {
            var observable = Observable.Generate(
                            0,
                            i => i < 100,
                            i => i + 1,
                            i => i + 1,
                            t => TimeSpan.FromMilliseconds(200));

            var bufferO = observable.Buffer(TimeSpan.FromSeconds(1));

            bufferO.Subscribe(buffer =>
            {
                foreach (var b in buffer)
                {
                    TestContext.Progress.Write(b + ";");
                }
                TestContext.Progress.WriteLine();
            });

            Task.Delay(TimeSpan.FromSeconds(10)).Wait();
        }

        [Ignore("less important")]
        [TestCase]
        public void A31_GenerateWithTime_Group()
        {
            var o1 = Observable.Generate(0,
                i => i < 100,
                i => i + 1,
                i => i % 5,
                i => TimeSpan.FromSeconds(0.01));

            var buffers = o1
                .Select(num => new Tuple<int, DateTime>(num, DateTime.Now))
                .Buffer(TimeSpan.FromMilliseconds(250));

            buffers.Subscribe(list =>
            {
                IEnumerable<Tuple<int, DateTime>> groups =
                    list
                        .GroupBy(tuple => tuple.Item1)
                        .Select(group => @group.ToList())
                        .Select(dataList => dataList.Last());

                foreach (var tuple in groups)
                {
                    TestContext.Progress.WriteLine($"number: {tuple.Item1},last element in buffer ms time: {tuple.Item2.Millisecond}");
                }
            });

            Task.Delay(TimeSpan.FromSeconds(5)).Wait();
        }

        [TestCase]
        public async Task A32_Sample()
        {
            var numbersStream = Observable.Generate(0,
                i => i < 100,
                i => i + 1,
                i => i,
                i => TimeSpan.FromMilliseconds(10));

            EventLoopScheduler els = new EventLoopScheduler();

            numbersStream.Subscribe(num => TestContext.Progress.WriteLine($"not sampled: {num}"));

            numbersStream
                .Sample(TimeSpan.FromMilliseconds(100))
                .ObserveOn(els)
                .Subscribe(num => TestContext.Progress.WriteLine($"With sampling: {num}"));

            await Task.Delay(1000);
        }

        [Ignore("less important")]
        [TestCase]
        public void A33_CheckSample2()
        {
            ISubject<int> subject = new Subject<int>();

            subject.Sample(TimeSpan.FromSeconds(1)).Subscribe(num => TestContext.Progress.WriteLine(num));

            for (int i = 0; i < 100; i++)
            {
                subject.OnNext(1);
                Thread.Sleep(100);
            }
        }
        [TestCase]
        public void A34_GenerateWithTime_GroupBy()
        {
            var source = Observable.Interval(TimeSpan.FromMilliseconds(10)).Take(100);

            var groups = source.GroupBy(i => i % 3);

            groups.ForEachAsync(group => group.Sample(TimeSpan.FromMilliseconds(200))
                .Subscribe(num => TestContext.Progress.WriteLine($"number during group sample:{num}, group sample time:{DateTime.Now.ToString("HH:mm:ss.fff")}")));

            Task.Delay(TimeSpan.FromSeconds(2)).Wait();
        }

        [TestCase]
        public void A35_Throttle()
        {
            var numbersStream = Observable.Generate(0,
                i => i < 12,
                i => i + 1,
                i => i,
                i => TimeSpan.FromSeconds(i/2.0));

            numbersStream.Subscribe(num => TestContext.Progress.WriteLine($"no throttle: {num}, {DateTime.Now.ToString("HH:mm:ss.fff")}"));

            numbersStream.Throttle(TimeSpan.FromSeconds(2))
                .Subscribe(num => TestContext.Progress.WriteLine($"with throttle: {num},{DateTime.Now.ToString("HH:mm:ss.fff")} - 2 seconds of quite"));

            Task.Delay(TimeSpan.FromSeconds(10)).Wait();
        }

        [Ignore("less important")]
        [TestCase]
        public void A36_Defer()
        {
            var obs = Observable.Return(DateTime.Now);
            // .Defer() create observable only after subscription
            var deferObs = Observable.Defer(() => Observable.Return(DateTime.Now));

            obs.Subscribe(dt => TestContext.Progress.WriteLine($"{DateTime.Now.ToLocalTime()} not defered: {dt}"));
            deferObs.Subscribe(dt => TestContext.Progress.WriteLine($"{DateTime.Now.ToLocalTime()} defered: {dt}"));
            
            Task.Delay(TimeSpan.FromSeconds(2)).Wait();
            
            obs.Subscribe(dt => TestContext.Progress.WriteLine($"{DateTime.Now.ToLocalTime()} not defered: {dt}"));
            deferObs.Subscribe(dt => TestContext.Progress.WriteLine($"{DateTime.Now.ToLocalTime()} defered: {dt}"));

        }

        [TestCase]
        public void A37_asyncSubscription()
        {
            async Task printNumber(int p_number)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50)).ContinueWith(t => TestContext.Progress.WriteLine(p_number));
            }

            IObservable<int> generatedObservables = Observable.Generate(1,
                i => i < 10,
                i => i + 1,
                i => i*i,
                i => TimeSpan.FromSeconds(0.5));

            generatedObservables.Subscribe(async i => await printNumber(i));
            Task.Delay(TimeSpan.FromSeconds(5)).Wait();
        }

        [Ignore("less important")]
        [TestCase]
        public void A38_SwitchObservables()
        {
            IObservable<int> GenerateObservable(int i)
            {
                return Observable.Generate(
                    i,
                    j => j < 10, 
                    j => j, 
                    j => j , 
                    j => TimeSpan.FromSeconds(0.5));
            }

            IObservable<IObservable<int>> observablesOfObservables = Observable.Generate(1,
                i => i < 10,
                i => i + 1,
                i => GenerateObservable(i),
                i => TimeSpan.FromSeconds(0.5));

            // switch() - Transforms an observable sequence of observable sequences into an observable sequence producing values only from the most recent observable sequence
            var singleObs = observablesOfObservables.Switch();          
            singleObs.Subscribe(i => TestContext.Progress.WriteLine(i));

            Task.Delay(TimeSpan.FromSeconds(5)).Wait();
        }

        #endregion

        #region Fields

        #endregion
    }
}