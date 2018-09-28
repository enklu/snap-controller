using System;
using Akka.Actor;
using Serilog;

namespace CreateAR.Snap
{
    public class CaptureActor : ReceiveActor
    {
        private readonly IActorRef _listener;

        public CaptureActor(IActorRef listener)
        {
            _listener = listener;

            Receive<ImageProcessingPipelineActor.Capture>(msg =>
            {
                Log.Information("Starting capture.");

                // TODO: ... capture

                // STUB
                Context.System.Scheduler.ScheduleTellOnce(
                    TimeSpan.FromSeconds(1),
                    _listener,
                    new ImageProcessingPipelineActor.CaptureComplete
                    {
                        Snap = msg.Snap
                    },
                    null);
            });
        }
    }

    public class ComposeActor : ReceiveActor
    {
        private readonly IActorRef _listener;

        public ComposeActor(IActorRef listener)
        {
            _listener = listener;

            Receive<ImageProcessingPipelineActor.Compose>(msg =>
            {
                Log.Information("Starting compose.");

                // TODO: ... compose

                // STUB
                Context.System.Scheduler.ScheduleTellOnce(
                    TimeSpan.FromSeconds(1),
                    _listener,
                    new ImageProcessingPipelineActor.ComposeComplete
                    {
                        Snap = msg.Snap
                    },
                    null);
            });
        }
    }

    public class PostActor : ReceiveActor
    {
        private readonly IActorRef _listener;

        public PostActor(IActorRef listener)
        {
            _listener = listener;

            Receive<ImageProcessingPipelineActor.Post>(msg =>
            {
                Log.Information("Starting post.");

                // TODO: ... post

                // STUB
                Context.System.Scheduler.ScheduleTellOnce(
                    TimeSpan.FromSeconds(1),
                    _listener,
                    new ImageProcessingPipelineActor.PostComplete
                    {
                        Snap = msg.Snap
                    },
                    null);
            });
        }
    }

    public class ImageProcessingPipelineActor : ReceiveActor
    {
        public struct SnapRecord
        {

        }

        public class StartPipeline
        {
            public SnapRecord Snap;
        }

        public class Capture
        {
            public SnapRecord Snap;
        }

        public class CaptureComplete
        {
            public SnapRecord Snap;
        }

        public class Compose
        {
            public SnapRecord Snap;
        }

        public class ComposeComplete
        {
            public SnapRecord Snap;
        }

        public class Post
        {
            public SnapRecord Snap;
        }

        public class PostComplete
        {
            public SnapRecord Snap;
        }

        private readonly IActorRef _captureRef;
        private readonly IActorRef _composeRef;
        private readonly IActorRef _postRef;

        public ImageProcessingPipelineActor()
        {
            _captureRef = Context.ActorOf(Props.Create(() => new CaptureActor(Self)));
            _composeRef = Context.ActorOf(Props.Create(() => new ComposeActor(Self)));
            _postRef = Context.ActorOf(Props.Create(() => new PostActor(Self)));

            Receive<StartPipeline>(msg =>
            {
                Log.Information("Starting pipeline.", msg.Snap);

                _captureRef.Tell(new Capture
                {
                    Snap = msg.Snap
                });
            });

            Receive<CaptureComplete>(msg => {
                Log.Information("Moving along to compose.", msg.Snap);

                _composeRef.Tell(new Compose
                {
                    Snap = msg.Snap
                });
            });

            Receive<ComposeComplete>(msg =>
            {
                Log.Information("Moving along to post.", msg.Snap);

                _postRef.Tell(new Post
                {
                    Snap = msg.Snap
                });
            });

            Receive<PostComplete>(msg =>
            {
                Log.Information("Pipeline complete!", msg.Snap);
            });
        }
    }
}
