using ChessCommon;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessGui {
    public abstract class ChessGui : Game {
        protected GraphicsDeviceManager graphics;
        protected GameStateRenderer renderer;


        public ChessGui() {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }


        protected override void Initialize() {
            Window.AllowUserResizing = true;

            renderer = new GameStateRenderer();
            renderer.parent = this;

            renderer.Initialize();

            base.Initialize();
        }

        protected override void LoadContent() {
            renderer.spriteBatch = new SpriteBatch(GraphicsDevice);

            renderer.LoadContent(Content);
        }

        protected override void Update(GameTime gameTime) {
            renderer.Update(gameTime);

            base.Update(gameTime);
        }

        protected void BaseUpdate(GameTime gameTime) {
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            renderer.Draw(gameTime, GraphicsDevice);

            base.Draw(gameTime);
        }

        protected void BaseDraw(GameTime gameTime) {
            base.Draw(gameTime);
        }




        public abstract ColourRights GetRights();

        public abstract void SubmitMoves();

        public abstract void GuiUndoMove();

        public abstract void MakeMove(Move move);

    }
}
