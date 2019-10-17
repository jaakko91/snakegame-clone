using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace SnakeGame
{

    public class Game1 : Game
    {

        //Size of sprite graphics (square)
        const int spriteSize = 32;

        //Number of sprites the play area is made of
        const int playfieldSize = 30;

        //Empty space in left side of window for text to be shown
        const int leftOffset = 210;
        
        //Size for window in menus
        const int menuWindowSize = 450;

        /*Array that holds values for graphics to be drawn
        0 = empty
        1 = wall
        2 = snakehead
        3 = snaketail
        4 = apple
        */
        int[,] playField = new int[playfieldSize, playfieldSize];

        //Keep track of game update speed
        long tickCount;

        //Variables used to blink and animate some things
        long animationTimer;
        bool blinkText; 

        //How fast the snake moves
        //Delay in milliseconds, the lower the faster
        float gameSpeed;

        /*Next move
        1 = up
        2 = right
        3 = down
        4 = left
        */
        int nextMove;

        //Snake buffer used to store positions of snake tail
        int[] snakeXbuffer = new int[501];
        int[] snakeYbuffer = new int[501];

        int score, snakeX, snakeY, snakeLength;

        //Arrays to store apple positions (there can be multiple apples at the same time)
        int[] appleX = new int[10];
        int[] appleY = new int[10];

        //Difficulty level in string format to show on screen
        string difficulty;

        //Random number generator
        Random getrandom = new Random();

        bool gamePaused, gameOver;

        //Information about game state (in game, menu etc.)
        int gameState;

        int windowHeight;
        int windowWidth;

        Texture2D wall, ground, snakehead, snaketail, apple;
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont font1;
        KeyboardState oldState, currentState;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            //Calculate window dimensions for in-game based on sprite dimensions and number of sprites being drawn and add offset on left side
            windowHeight = spriteSize * playfieldSize;
            windowWidth = spriteSize * playfieldSize + leftOffset;
            //Set window dimensions
            graphics.PreferredBackBufferWidth = menuWindowSize;
            graphics.PreferredBackBufferHeight = menuWindowSize;
            graphics.ApplyChanges();

            //Enter main menu
            gameState = 1;
        }

        //Reset playfield and all variables
        private void resetGame()
        {
            
            //Clear the playfield
            for (int i = 0; i < playfieldSize; i++)
            {
                for (int j = 0; j < playfieldSize; j++)
                {
                    playField[i, j] = 0;
                }
            }

            //Add walls to the map (so each edge has walls)
            for (int i = 0; i < playfieldSize; i++)
            {
                for (int j = 0; j < playfieldSize; j++)
                {
                    if (i == 0 || j == 0 || i == playfieldSize - 1 || j == playfieldSize - 1)  
                            playField[i, j] = 1;       
                }
            }

            //Add a few random wall blocks to the middle of the map
            for (int i = 0; i < playfieldSize; i++)
            {
                for (int j = 0; j < playfieldSize; j++)
                {
                    if (getrandom.Next(1, 25) == 1)
                        playField[i, j] = 1;
                }
            }

            //Make sure there are no obstacles in front of the snake when game begins
            for (int i = 0; i < 10; i++)
            {
                if (playField[15 + i, 15] == 1)
                    playField[15 + i, 15] = 0;
            }

            //Reset snake buffer
            for (int i = 0; i < 500; i++)
            {
                snakeXbuffer[i] = 0;
                snakeYbuffer[i] = 0;
            }

            //Reset apple array. -1 means no apple.
            for (int i = 0; i < 10; i++)
            {
                appleX[i] = -1;
                appleY[i] = -1;
            }

            //Init timing system
            tickCount = 0;
            //Init game speed
            gameSpeed = 300;
            //Init next move, start by moving right
            nextMove = 2;
            //Init snake position and length
            snakeX = 15;
            snakeY = 15;
            snakeLength = 0;
            //Init score and difficulty
            score = 0;
            difficulty = "I";

            animationTimer = 0;

            //Start game with two generated apples
            generateApple();
            generateApple();
            
            //Start game
            gamePaused = false;
            gameOver = false;

        }

        //Finds a free memory from apple array and inserts new apple coordinates to the array.
        //Returns true if it was successful.
        private bool insertApple(int newAppleX, int newAppleY)
        {
            for (int i = 0; i < 10; i++)
            {
                //Found free spot for apple, insert new apple to array and return true.
                if (appleX[i] == -1 && appleY[i] == -1)
                {
                    appleX[i] = newAppleX;
                    appleY[i] = newAppleY;
                    return true;
                }
            }
            //Array is full, there are already 10 apples on the playfield.
            return false;
        }

        //Search array if apple exists in specific coordinates.
        //Returns -1 if not found, otherwise returns the number of the memory place in array where it was found.
        private int searchApple(int x, int y)
        {
            for (int i = 0; i < 10; i++)
            {
                if (appleX[i] == x && appleY[i] == y)
                    return i;
            }
            return -1;   
        }

        //Removes the apple which number is passed as a parameter from the list.
        private void removeApple(int n)
        {
            appleX[n] = -1;
            appleY[n] = -1;
        }

        //Spawn apple and make sure it is not inside wall or another apple
        private void generateApple()
        {
            int tempX = -1;
            int tempY = -1;
            bool posOK = false;

            while (!posOK)
            {
                //Position between 1-28 in playfield
                tempX = getrandom.Next(0, 27) + 1;
                tempY = getrandom.Next(0, 27) + 1;
                
                //Check that position is empty on the field (no wall)
                if (playField[tempX, tempY] == 0)
                {
                    //Check that apple is reachable (so that there is entry and exit point around apple)
                    int wallCount = playField[tempX,(tempY - 1)] + playField[(tempX + 1), tempY] + playField[tempX,(tempY + 1)] + playField[(tempX - 1), tempY];
                    if (wallCount <= 2)
                        {
                            //Check that there isn't apple already at that position. If no, then position is OK, exit loop.
                            if (searchApple(tempX, tempY) == -1)
                                posOK = true;
                        }

                            
                }
            }
            //Insert apple to array.
            insertApple(tempX, tempY);
        }

        //Update difficulty level based on score. If difficulty level increases, spawn a new apple.
        //That way there will always be one more apple, since new one is always generated when one is eaten.
        private void updateDifficulty()
        {
            float oldSpeed = gameSpeed;

            if (score >= 10 && score < 100)
            {
                difficulty = "II";
                gameSpeed = 250;
            }
            else if (score >= 100 && score < 200)
            {
                difficulty = "III";
                gameSpeed = 220;
            }
            else if (score >= 200 && score < 500)
            {
                difficulty = "IIII";
                gameSpeed = 180;
            }
            else if (score >= 500 && score < 1000)
            {
                difficulty = "IIIII";
                gameSpeed = 150;
            }
            else if (score >= 1000)
            {
                difficulty = "IIIIII";
                gameSpeed = 110;
            }

            if (oldSpeed != gameSpeed)
                generateApple();
        }
        
        /*Check collision for new position, return value:
        0 = no collision
        1 = wall
        2 = worm tail
        3 = apple
        */
        private int collisionCheck(int[,] map, int x, int y)
        {
            int t = searchApple(x, y);
            if (t != -1)
            {
                removeApple(t);
                return 3;
            }
            /*
            //Collision check with apples
            for (int i = 0; i < 10; i++)
            {
                if (appleX[i] == x && appleY[i] == y)
                    return 3;
            }
            */
            //Collision check with worm tail
            for (int i = 0; i < snakeLength; i++)
            {
                if (snakeX == snakeXbuffer[i] && snakeY == snakeYbuffer[i])
                    return 2;
            }

            //Collision check with wall
            if (map[x, y] == 1)
                return 1;

            //If we got here, there were no collisions, return 0
            return 0;
        }

        //Draw snake
        private void drawSnake()
        {
            //Draw snake tail
            for (int i = 0; i < snakeLength; i++)
            {
                if ( !(snakeXbuffer[i] == 0 || snakeYbuffer[i] == 0))
                    spriteBatch.Draw(snaketail, new Vector2(snakeXbuffer[i] * spriteSize + leftOffset, snakeYbuffer[i] * spriteSize), Color.White);
            }
            //Draw head
            spriteBatch.Draw(snakehead, new Vector2(snakeX * spriteSize + leftOffset, snakeY * spriteSize), Color.White);
        }

        //Draw apples
        private void drawApple()
        {
            for (int i = 0; i < 10; i++)
            {
                if (appleX[i] != -1 && appleY[i] != -1)
                    spriteBatch.Draw(apple, new Vector2(appleX[i] * spriteSize + leftOffset, appleY[i] * spriteSize), Color.White);
            }
        }

        //Draw playfield
        private void drawMap()
        {
            for (int i = 0; i < playfieldSize; i++)//X
            {
                for (int j = 0; j < playfieldSize; j++)//Y 
                {
                    //Calculate position for sprite
                    int posX = i * spriteSize + leftOffset;
                    int posY = j * spriteSize;
                    //Draw correct sprite
                    switch (playField[i, j])
                    {
                        case 0://Empty
                            break;

                        case 1://Wall
                            spriteBatch.Draw(wall, new Vector2(posX, posY), Color.White);
                            break;
                            
                        default:
                            break;
                        //Possible to add different blocks later...
                    }

                }
            }

            //Draw texts on left side of the playfield
            spriteBatch.DrawString(font1, "Score:\n" + score.ToString("D5"), new Vector2(5, 30), Color.LightGray);
            spriteBatch.DrawString(font1, "Length:\n" + snakeLength.ToString("D3"), new Vector2(5, 130), Color.LightGray);
            spriteBatch.DrawString(font1, "Difficulty:\n" + difficulty, new Vector2(5, 230), Color.LightGray);
        }

        /// MonoGame
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        protected override void Initialize()
        {
            resetGame();
            //Pause game when first time entering main menu
            gamePaused = true;
            base.Initialize();
        }

        /// MonoGame
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        protected override void LoadContent()
        {
            //Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            //Load sprites
            wall = Content.Load<Texture2D>("wall");
            ground = Content.Load<Texture2D>("ground");
            snakehead = Content.Load<Texture2D>("snakehead");
            snaketail = Content.Load<Texture2D>("snaketail");
            apple = Content.Load<Texture2D>("apple");
            font1 = Content.Load<SpriteFont>("generalfont");
        }

        /// MonoGame
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        protected override void UnloadContent()
        {
        }

        /// MonoGame
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        protected override void Update(GameTime gameTime)
        {
            //Store previous keyboard state to prevent debouncing problems with some keys (not used with all keys)
            oldState = currentState;
            currentState = Keyboard.GetState();

            switch (gameState)
            {
                case 1: //Main menu
                    if (Keyboard.GetState().IsKeyDown(Keys.F2)) //Start new game
                        gameState = 2;

                    if (Keyboard.GetState().IsKeyDown(Keys.Escape)) //Exit
                        Exit();
                    break;

                case 2: //Game starting
                    resetGame();
                    //Set window dimensions
                    graphics.PreferredBackBufferWidth = windowWidth;
                    graphics.PreferredBackBufferHeight = windowHeight;
                    graphics.ApplyChanges();
                    //Set game running state
                    gameState = 3;
                    break;

                case 3: //Game running
                    //Record key presses
                    if (Keyboard.GetState().IsKeyDown(Keys.Up))
                    {
                        //Don't allow worm to turn 180 degrees
                        if (nextMove != 3)
                            nextMove = 1;
                    }
                    else if (Keyboard.GetState().IsKeyDown(Keys.Right))
                    {
                        if (nextMove != 4)
                            nextMove = 2;
                    }
                    else if (Keyboard.GetState().IsKeyDown(Keys.Down))
                    {
                        if (nextMove != 1)
                            nextMove = 3;
                    }
                    else if (Keyboard.GetState().IsKeyDown(Keys.Left))
                    {
                        if (nextMove != 2)
                            nextMove = 4;
                    }
                    if (currentState.IsKeyDown(Keys.R) && oldState.IsKeyUp(Keys.R))//Restart game
                    {
                        resetGame();
                    }
                    break;

                case 4: //Game over
                    if(Keyboard.GetState().IsKeyDown(Keys.F1))//Go back to main menu
                    {
                        //Set main menu state
                        gameState = 1;
                        //Set window dimensions
                        graphics.PreferredBackBufferWidth = menuWindowSize;
                        graphics.PreferredBackBufferHeight = menuWindowSize;
                        graphics.ApplyChanges();
                    }
                    break;

                default:
                    break;
            }

            tickCount = tickCount += gameTime.ElapsedGameTime.Milliseconds;

            //Check when it's time to move/update snake
            if ((tickCount >= gameSpeed) && !gamePaused && !gameOver)
            {
                tickCount = 0;

                if (snakeLength == 1) //Tail length 1, just store last coordinates of worm head
                {
                    snakeXbuffer[0] = snakeX;
                    snakeYbuffer[0] = snakeY;
                }
                else if (snakeLength > 1) //Longer tail, move everything back in array and store new coordinates to first position
                {
                    for (int i = 0; i < (snakeLength-1); i++)
                    {
                        snakeXbuffer[snakeLength - i - 1] = snakeXbuffer[snakeLength - i - 2];
                        snakeYbuffer[snakeLength - i - 1] = snakeYbuffer[snakeLength - i - 2];
                    }
                    snakeXbuffer[0] = snakeX;
                    snakeYbuffer[0] = snakeY;
                }

                //Move snake based on input
                switch (nextMove)
                {
                    //Up
                    case 1:
                        snakeY -= 1;
                        break;
                    //Right
                    case 2:
                        snakeX += 1;
                        break;
                    //Down
                    case 3:
                        snakeY += 1;
                        break;
                    //Left
                    case 4:
                        snakeX -= 1;
                        break;
                    default:
                        break;
                }  

                //Check collisions
                int col = collisionCheck(playField, snakeX, snakeY);
                if (col == 1) //Hit wall
                {
                    gameOver = true;
                    gameState = 4;
                }
                else if (col == 2) //Hit worm tail
                {
                    gameOver = true;
                    gameState = 4;
                }
                else if (col == 3) //Collected apple
                {
                    if(snakeLength<500)
                        snakeLength++;
                    score = score + snakeLength;
                    generateApple();
                    updateDifficulty();
                }
            }
            base.Update(gameTime);
        }

        /// MonoGame
        /// This is called when the game should draw itself.
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin();

            //Select what is drawn based on game state
            switch (gameState)
            {
                case 1: //Main menu
                    spriteBatch.DrawString(font1, "F2 = NEW GAME", new Vector2(100, 150), Color.White);
                    spriteBatch.DrawString(font1, "ESCAPE = EXIT", new Vector2(110, 200), Color.White);
                    break;

                case 2: //Game starting
                    break;

                case 3: //Game running
                    drawMap();
                    drawSnake();
                    drawApple();
                    break;

                case 4: //Game over
                    //Blink "game over" text and worm
                    animationTimer++;
                    if (animationTimer > 40)
                    {
                        if (blinkText)
                            blinkText = false;
                        else
                            blinkText = true;

                        animationTimer = 0;
                    }
                    if(blinkText)
                    {
                        drawSnake();
                        spriteBatch.DrawString(font1, "GAME OVER!", new Vector2(5, 350), Color.White);
                    }   
                    drawMap();
                    drawApple();
                    spriteBatch.DrawString(font1, "Press F1 to", new Vector2(5, 400), Color.White);
                    spriteBatch.DrawString(font1, "go back to", new Vector2(5, 430), Color.White);
                    spriteBatch.DrawString(font1, "main menu", new Vector2(5, 460), Color.White);
                    break;

                default:
                break;
            }  

            spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}