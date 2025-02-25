﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Online;
using osu.Game.Online.API;
using osu.Game.Overlays.BeatmapListing.Panels;
using osu.Game.Resources.Localisation.Web;
using osu.Game.Users;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Overlays.BeatmapSet.Buttons
{
    public class HeaderDownloadButton : BeatmapDownloadTrackingComposite, IHasTooltip
    {
        private const int text_size = 12;

        private readonly bool noVideo;

        public LocalisableString TooltipText => BeatmapsetsStrings.ShowDetailsDownloadDefault;

        private readonly IBindable<User> localUser = new Bindable<User>();

        private ShakeContainer shakeContainer;
        private HeaderButton button;

        public HeaderDownloadButton(BeatmapSetInfo beatmapSet, bool noVideo = false)
            : base(beatmapSet)
        {
            this.noVideo = noVideo;

            Width = 120;
            RelativeSizeAxes = Axes.Y;
        }

        [BackgroundDependencyLoader]
        private void load(IAPIProvider api, BeatmapManager beatmaps)
        {
            FillFlowContainer textSprites;

            AddInternal(shakeContainer = new ShakeContainer
            {
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = 5,
                Child = button = new HeaderButton { RelativeSizeAxes = Axes.Both },
            });

            button.AddRange(new Drawable[]
            {
                new Container
                {
                    Padding = new MarginPadding { Horizontal = 10 },
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        textSprites = new FillFlowContainer
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            AutoSizeAxes = Axes.Both,
                            AutoSizeDuration = 500,
                            AutoSizeEasing = Easing.OutQuint,
                            Direction = FillDirection.Vertical,
                        },
                        new SpriteIcon
                        {
                            Anchor = Anchor.CentreRight,
                            Origin = Anchor.CentreRight,
                            Icon = FontAwesome.Solid.Download,
                            Size = new Vector2(18),
                        },
                    }
                },
                new DownloadProgressBar(BeatmapSet.Value)
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                },
            });

            button.Action = () =>
            {
                if (State.Value != DownloadState.NotDownloaded)
                {
                    shakeContainer.Shake();
                    return;
                }

                beatmaps.Download(BeatmapSet.Value, noVideo);
            };

            localUser.BindTo(api.LocalUser);
            localUser.BindValueChanged(userChanged, true);
            button.Enabled.BindValueChanged(enabledChanged, true);

            State.BindValueChanged(state =>
            {
                switch (state.NewValue)
                {
                    case DownloadState.Downloading:
                        textSprites.Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Text = Localisation.CommonStrings.Downloading,
                                Font = OsuFont.GetFont(size: text_size, weight: FontWeight.Bold)
                            },
                        };
                        break;

                    case DownloadState.Importing:
                        textSprites.Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Text = Localisation.CommonStrings.Importing,
                                Font = OsuFont.GetFont(size: text_size, weight: FontWeight.Bold)
                            },
                        };
                        break;

                    case DownloadState.LocallyAvailable:
                        this.FadeOut(200);
                        break;

                    case DownloadState.NotDownloaded:
                        textSprites.Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Text = BeatmapsetsStrings.ShowDetailsDownloadDefault,
                                Font = OsuFont.GetFont(size: text_size, weight: FontWeight.Bold)
                            },
                            new OsuSpriteText
                            {
                                Text = getVideoSuffixText(),
                                Font = OsuFont.GetFont(size: text_size - 2, weight: FontWeight.Bold)
                            },
                        };
                        this.FadeIn(200);
                        break;
                }
            }, true);
        }

        private void userChanged(ValueChangedEvent<User> e) => button.Enabled.Value = !(e.NewValue is GuestUser);

        private void enabledChanged(ValueChangedEvent<bool> e) => this.FadeColour(e.NewValue ? Color4.White : Color4.Gray, 200, Easing.OutQuint);

        private LocalisableString getVideoSuffixText()
        {
            if (!BeatmapSet.Value.OnlineInfo.HasVideo)
                return string.Empty;

            return noVideo ? BeatmapsetsStrings.ShowDetailsDownloadNoVideo : BeatmapsetsStrings.ShowDetailsDownloadVideo;
        }
    }
}
