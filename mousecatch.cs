using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using System.Threading;

public class mousecatch : MonoBehaviour
{

    private GameObject finder, finderanother, chess, nextchess, followchess, cbase;
    public Texture btnback, btnhelp;

    public GUIStyle gooder;
    public GUIStyle challenger;
    public GameObject[] prefab = new GameObject[6];
    public GameObject recev;
    public AudioClip AC, BC, CC, DC, endAC, returnAC, chalengewinAC, normalwinAC;
    public Texture restarting;
    public Font fonter;
    public int vsmode = 0;
    private int sander = 0, whowin = 0, poping_history = 0, dosander = 0;
    private int sending = 0;
    int lx = -1, ly = -1;
    int challenge_mode = 0;
    //贪吃权重,禁锢敌方,反击能力
    int[] score_scale = { 700, 400, 700 };
    files filer = new files();
    Stack<Vector2Int> flipstack = new Stack<Vector2Int>();
    Stack<Thread> thread_actnow = new Stack<Thread>();
    int[,] chessboard = new int[8, 8];
    int[,] imagineboard = new int[8, 8];
    string output_str;
    bool[,] flip_possible_map = new bool[8, 8];

    Stack<Stack<Vector2Int>> history = new Stack<Stack<Vector2Int>>();

    Stack<Vector2Int>[,] nowpossible_global;
    int[,] cpboard_global = new int[8, 8];
    int[] scale_global = new int[3];
    int wait_num_global = 0;
    int current_return_global = 0;
    int[,] score_map_global = new int[8, 8];

    int history_id = 0;
    int nextcolor = 1;
    int player_chess = 0;
    int tropical_side = -1;
    int you_win_time = 0;
    int current_round = 0;
    int wait_is_on = 0;
    int is_my_turn = 0;
    int gameover = 0;
    int big_button_size;
    int bar_height;
    int proper_fontsize;
    int whitescore, blackscore;
    // Use this for initialization
    void Start()
    {

        big_button_size = this.GetComponent<proper_ui>().proper_big_button;
        bar_height = this.GetComponent<proper_ui>().proper_bar_height;
        proper_fontsize = this.GetComponent<proper_ui>().proper_font_size;
        this.GetComponent<proper_ui>().set_proper_ui_style(gooder);
        this.GetComponent<proper_ui>().set_proper_ui_style(challenger);
        string sidestr = filer.ReadTextFile("turn.txt");
        if (sidestr == "black")
        {
            tropical_side = 1;
        }
        cbase = GameObject.FindGameObjectWithTag("camerabase");
        initboard();

    }
    void initboard()
    {
        clear_board();
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                chessboard[i, j] = 0;
            }
        dosander = 0;
        whowin = 0;
        nextcolor = 1;

        for (int i = 3; i < 5; i++)
            for (int j = 3; j < 5; j++)
            {
                if (i == j)
                {
                    put_a_chess_to_board(i, j, -1);
                }
                else
                {
                    put_a_chess_to_board(i, j, 1);
                }
            }
        if (tropical_side == 1)
        {
            let_me_play();
        }
        else
        {
            search_possible_position_and_flushlight(nextcolor);
        }

    }
    void clear_board()
    {
        clear_light();
        GameObject[] remains = GameObject.FindGameObjectsWithTag("chesser");
        foreach (GameObject re in remains)
        {
            Destroy(re.gameObject);
        }
        whitescore = 0;
        blackscore = 0;
    }
    void clear_light()
    {
        GameObject[] remains = GameObject.FindGameObjectsWithTag("lighter");
        foreach (GameObject re in remains)
        {
            Destroy(re.gameObject);
        }

        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
                flip_possible_map[i, j] = false;
    }
    void copyboard(int[,] destination_board, int[,] from_board)
    {
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                destination_board[i, j] = from_board[i, j];
            }
    }
    void initboard_from(int[,] from_board)
    {
        clear_board();
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                chessboard[i, j] = from_board[i, j];
                if (chessboard[i, j] != 0)
                    put_a_chess_to_board(i, j, chessboard[i, j]);
            }
    }
    void push_current_to_history(Vector2Int clicker, Stack<Vector2Int> flipper)
    {
        Stack<Vector2Int> currentop = new Stack<Vector2Int>();

        foreach (Vector2Int nc in flipper)
        {
            currentop.Push(nc);
        }
        currentop.Push(clicker);

        history.Push(currentop);
    }
    void pop_history()
    {
        if (history.Count <= 0)
            return;
        Stack<Vector2Int> currentop = history.Peek();
        Vector2Int delaim = currentop.Peek();
        currentop.Pop();
        GameObject lastchess = GameObject.Find(chessname(delaim.x, delaim.y));
        Destroy(lastchess.gameObject);
        if (chessboard[delaim.x, delaim.y] == 1)
        {
            blackscore--;
        }
        else
        {
            whitescore--;
        }
        chessboard[delaim.x, delaim.y] = 0;

        foreach (Vector2Int flipagain in currentop)
        {
            flip_the_chess_at(flipagain);
        }
        nextcolor *= -1;
        search_possible_position_and_flushlight(nextcolor);
        history.Pop();
    }
    int search_possible_map(int color)
    {
        int possible_num = 0;
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (chessboard[i, j] == 0)
                {
                    Stack<Vector2Int> rev = could_flip_at(chessboard, i, j, color);
                    if (rev.Count > 0)
                    {
                        flip_possible_map[i, j] = true;
                        possible_num++;
                    }
                }
            }
        }
        return possible_num;
    }
    void light_possible_map()
    {
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (flip_possible_map[i, j])
                {
                    GameObject lightobj = Instantiate(prefab[4]) as GameObject;
                    lightobj.transform.localPosition = array_pos_to_vector_light(i, j);
                    lightobj.name = "light" + i + "," + j;
                }
            }
        }
    }
    Stack<Vector2Int> could_flip_at(int[,] chessboard_t, int posx, int posy, int color)
    {
        // print("checking " + posx + "," + posy);
        Stack<Vector2Int> res = new Stack<Vector2Int>();
        if (posx < 6)
        {
            if (chessboard_t[posx + 1, posy] == -color)
            {
                int fini = -1;
                for (int i = posx + 2; i < 8; i++)
                {
                    if (chessboard_t[i, posy] == 0)
                    {
                        break; // no space is tolerated
                    }
                    if (chessboard_t[i, posy] == color)
                    {
                        fini = i; // find another same color chess
                        break;
                    }
                }
                for (int j = posx + 1; j < fini; j++)
                {
                    res.Push(new Vector2Int(j, posy));
                }
            }
        }

        if (posx > 1)
        {
            if (chessboard_t[(posx - 1), posy] == -color)
            {
                int fini = 11;
                for (int i = posx - 2; i >= 0; i--)
                {
                    if (chessboard_t[i, posy] == 0)
                    {
                        break; // no space is tolerated
                    }
                    if (chessboard_t[i, posy] == color)
                    {
                        fini = i; // find another same color chess
                        break;
                    }
                }
                for (int j = posx - 1; j > fini; j--)
                {
                    res.Push(new Vector2Int(j, posy));
                }
            }
        }

        if (posy < 6)
        {
            if (chessboard_t[posx, (posy + 1)] == -color)
            {
                int fini = -1;
                for (int i = posy + 2; i < 8; i++)
                {
                    if (chessboard_t[posx, i] == 0)
                    {
                        break; // no space is tolerated
                    }
                    if (chessboard_t[posx, i] == color)
                    {
                        fini = i; // find another same color chess
                        break;
                    }
                }
                for (int j = posy + 1; j < fini; j++)
                {
                    res.Push(new Vector2Int(posx, j));
                }
            }
        }

        if (posy > 1)
        {
            if (chessboard_t[posx, (posy - 1)] == -color)
            {
                int fini = 11;
                for (int i = posy - 2; i >= 0; i--)
                {
                    if (chessboard_t[posx, i] == 0)
                    {
                        break; // no space is tolerated
                    }
                    if (chessboard_t[posx, i] == color)
                    {
                        fini = i; // find another same color chess
                        break;
                    }
                }
                for (int j = posy - 1; j > fini; j--)
                {
                    res.Push(new Vector2Int(posx, j));
                }
            }
        }

        if (posx < 6 && posy < 6)
        {
            int spx = 1;
            int spy = 1;
            if (chessboard_t[posx + spx, posy + spy] == -color)
            {
                int fini = -1;
                for (int i = 2; ; i++)
                {
                    int nx = posx + spx * i;
                    int ny = posy + spy * i;
                    if (nx >= 8 || ny >= 8)
                        break;
                    if (chessboard_t[nx, ny] == 0)
                        break;
                    if (chessboard_t[nx, ny] == color)
                    {
                        fini = i;
                        break;
                    }
                }
                for (int j = 1; j < fini; j++)
                {
                    int nx = posx + spx * j;
                    int ny = posy + spy * j;
                    res.Push(new Vector2Int(nx, ny));
                }
            }
        }

        if (posx > 1 && posy > 1)
        {
            int spx = -1;
            int spy = -1;
            if (chessboard_t[posx + spx, posy + spy] == -color)
            {
                int fini = -1;
                for (int i = 2; ; i++)
                {
                    int nx = posx + spx * i;
                    int ny = posy + spy * i;
                    if (nx < 0 || ny < 0)
                        break;
                    if (chessboard_t[nx, ny] == 0)
                        break;
                    if (chessboard_t[nx, ny] == color)
                    {
                        fini = i;
                        break;
                    }
                }
                for (int j = 1; j < fini; j++)
                {
                    int nx = posx + spx * j;
                    int ny = posy + spy * j;
                    res.Push(new Vector2Int(nx, ny));
                }
            }
        }

        if (posx > 1 && posy < 6)
        {
            int spx = -1;
            int spy = 1;
            if (chessboard_t[posx + spx, posy + spy] == -color)
            {
                int fini = -1;
                for (int i = 2; ; i++)
                {
                    int nx = posx + spx * i;
                    int ny = posy + spy * i;
                    if (nx < 0 || ny >= 8)
                        break;
                    if (chessboard_t[nx, ny] == 0)
                        break;
                    if (chessboard_t[nx, ny] == color)
                    {
                        fini = i;
                        break;
                    }
                }
                for (int j = 1; j < fini; j++)
                {
                    int nx = posx + spx * j;
                    int ny = posy + spy * j;
                    res.Push(new Vector2Int(nx, ny));
                }
            }
        }

        if (posx < 6 && posy > 1)
        {
            int spx = 1;
            int spy = -1;
            if (chessboard_t[posx + spx, posy + spy] == -color)
            {
                int fini = -1;
                for (int i = 2; ; i++)
                {
                    int nx = posx + spx * i;
                    int ny = posy + spy * i;
                    if (nx >= 8 || ny < 0)
                        break;
                    if (chessboard_t[nx, ny] == 0)
                        break;
                    if (chessboard_t[nx, ny] == color)
                    {
                        fini = i;
                        break;
                    }
                }
                for (int j = 1; j < fini; j++)
                {
                    int nx = posx + spx * j;
                    int ny = posy + spy * j;
                    res.Push(new Vector2Int(nx, ny));
                }
            }
        }

        return res;
    }

    //---------------------- Mouse Control -----------------------------------------------

    Vector3 mousetracker()
    {
        if (cbase == null)
            cbase = GameObject.FindGameObjectWithTag("camerabase");

        Vector3 camera = Camera.main.WorldToScreenPoint(cbase.transform.position);
        Vector3 pos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, camera.z);
        pos = Camera.main.ScreenToWorldPoint(pos);
        return pos;
    }
    Vector2Int catchmouse()
    {
        Vector3 pos = mousetracker();
        float cursor_x = pos.x;
        float cursor_y = pos.y;
        int cursor_array_x = 3 - (int)(cursor_y / 9.99f);
        int cursor_array_y = (int)((cursor_x - 5f) / 9.99f) + 4;
        if (pos.x > 5)
            cursor_array_y++;
        if (pos.y < 0)
            cursor_array_x++;

        return new Vector2Int(cursor_array_x, cursor_array_y);
        //   print("mouse at " + cursor_array_x + "," + cursor_array_y);

    }
    Vector3 array_pos_to_vector(int array_x, int array_y)
    {
        return new Vector3((array_y - 4) * 10f, (3 - array_x) * 10f + 5f, 79.5f);
    }
    Vector3 array_pos_to_vector_light(int array_x, int array_y)
    {
        return new Vector3((array_y - 4) * 10f, (3 - array_x) * 10f + 5f, 78f);
    }
    string chessname(int posx, int posy)
    {
        return "ch" + posx + "," + posy;
    }
    void put_a_chess_to_board(int posx, int posy, int color)
    {
        if (posx >= 0 && posx < 8 && posy >= 0 && posy < 8)
        {
            int whicher;
            if (color == -1)
            {
                whicher = 0;
                whitescore++;
            }
            else
            {
                whicher = 1;
                blackscore++;
            }
            GameObject newchess = Instantiate(prefab[whicher]);
            newchess.gameObject.name = chessname(posx, posy);
            chessboard[posx, posy] = color;
            newchess.transform.localPosition = array_pos_to_vector(posx, posy);
        }
    }
    void flip_the_chess_at(Vector2Int aimpos)
    {
        AudioSource.PlayClipAtPoint(DC, transform.localPosition);
        //   print("flip " + aimpos.x + "," + aimpos.y);
        GameObject thischess = GameObject.Find(chessname(aimpos.x, aimpos.y));
        thischess.GetComponent<flip>().flipping_op = 1;
        chessboard[aimpos.x, aimpos.y] *= -1;
        if (chessboard[aimpos.x, aimpos.y] == 1)
        {
            blackscore++;
            whitescore--;
        }
        else
        {
            whitescore++;
            blackscore--;
        }
    }
    void following()
    {

        Vector2Int nowposition = catchmouse();
        int xx = nowposition.x;
        int yy = nowposition.y;

        if (followchess == null)
        {
            if (xx >= 0 && xx < 8 && yy >= 0 && yy < 8)
            {
                if (chessboard[xx, yy] == 0 && flip_possible_map[xx, yy] == true)
                {
                    if (nextcolor == 1)
                        followchess = Instantiate(prefab[3]) as GameObject;
                    else
                    {
                        followchess = Instantiate(prefab[2]) as GameObject;
                    }
                }
            }
        }

        if (lx != xx || ly != yy)
        {
            if (followchess != null)
                Destroy(followchess.gameObject);
            if (nextcolor == 1)
                followchess = Instantiate(prefab[3]) as GameObject;
            else
            {
                followchess = Instantiate(prefab[2]) as GameObject;
            }

            lx = xx;
            ly = yy;

            if (xx >= 0 && xx < 8 && yy >= 0 && yy < 8)
            {
                if (chessboard[xx, yy] == 0 && flip_possible_map[xx, yy] == true)
                {
                    followchess.transform.position = array_pos_to_vector(xx, yy);
                }
                else
                {
                    Destroy(followchess.gameObject);
                }
            }
        }


    }
    void search_possible_position_and_flushlight(int color)
    {
        clear_light();
        int posinum = search_possible_map(color);
        if (posinum > 0)
        {
            light_possible_map();
        }
        else
        {
            int othercolor = -color;
            int ano = search_possible_map(othercolor);
            if (ano > 0)
            {
                output_str = "PASS";
                if (is_my_turn == 0)
                {
                    nextcolor *= -1;
                    is_my_turn = 30;
                }
            }
            else
            {
                if (whitescore > blackscore)
                {
                    gameover = -1;
                }
                if (blackscore > whitescore)
                {
                    gameover = 1;
                }
                if (blackscore == whitescore)
                {
                    gameover = 2;
                }
            }
        }
    }


    //---------------------- Update 60 times per second -----------------------------------------
    // Update is called once per frame
    void Update()
    {
        dosander++;

        if (is_my_turn > 0)
        {
            is_my_turn--;
            if (is_my_turn == 1)
                let_me_play();
        }

        if (dosander > 30)
            if (flipstack.Count > 0 && wait_is_on == 1)
            {
                foreach (Vector2Int flippos in flipstack)
                {
                    flip_the_chess_at(flippos);
                }
                flipstack.Clear();
                dosander = 0;
                search_possible_position_and_flushlight(nextcolor);
                wait_is_on = 0;
            }

        if (current_return_global == wait_num_global && wait_num_global > 0)
        {
            wait_num_global = 0;
            Vector2Int next_pos = new Vector2Int(0, 0);
            float maxscore = score_map_global[next_pos.x, next_pos.y];
            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++)
                {
                    if (score_map_global[i, j] > score_map_global[next_pos.x, next_pos.y])
                    {
                        next_pos.x = i;
                        next_pos.y = j;
                    }
                }
            if (next_pos.x >= 0)
            {
                flipstack = could_flip_at(chessboard, next_pos.x, next_pos.y, nextcolor);
                dosander = 0;
                put_a_chess_to_board(next_pos.x, next_pos.y, nextcolor);
                push_current_to_history(new Vector2Int(next_pos.x, next_pos.y), flipstack);
                wait_is_on = 1;
            }
            nextcolor *= -1;
            output_str += "I should pick " + next_pos.x + "," + next_pos.y;
            foreach (Thread ttr in thread_actnow) // 回收所有子线程
            {
                ttr.Abort();
            }
            thread_actnow.Clear();
        }

        if (gameover == 0)
        {

            if (is_my_turn == 0 && wait_num_global == 0)
                following();

            if (poping_history > 0)
            {
                poping_history--;
                dosander = 0;
                if (poping_history == 39 || poping_history == 1)
                {
                    pop_history();
                }
            }
            else
            {
                sander++;
            }
        }

        if (Input.GetMouseButton(0) && sander > 37 && gameover == 0 && wait_num_global == 0)
        {
            sander = 0;
            Vector2Int nowposition = catchmouse();
            int xx = nowposition.x;
            int yy = nowposition.y;
            if (xx >= 0 && xx < 8 && yy >= 0 && yy < 8)
                if (flip_possible_map[xx, yy] == true)
                {
                    flipstack = could_flip_at(chessboard, xx, yy, nextcolor);
                    if (flipstack.Count > 0)
                    {
                        put_a_chess_to_board(xx, yy, nextcolor);
                        foreach (Vector2Int flippos in flipstack)
                        {
                            flip_the_chess_at(flippos);
                        }
                        push_current_to_history(new Vector2Int(xx, yy), flipstack);
                        flipstack.Clear();
                        nextcolor *= -1;

                        is_my_turn = 30;
                    }
                }
        }

    }
    void let_me_play()
    {
        if (followchess != null)
            Destroy(followchess.gameObject);

        if (blackscore + whitescore == 64)
        {
            if (blackscore > whitescore)
                gameover = 1;
            if (whitescore > blackscore)
                gameover = -1;
            if (whitescore == blackscore)
                gameover = 2;

            return;
        }
        search_possible_position_and_flushlight(nextcolor);
        bool possible = analysis_board(chessboard, nextcolor, score_scale);
        if (!possible)
        {
            nextcolor *= -1;
        }
    }
    //---------------------- GUI -----------------------------------------

    void OnGUI()
    {
        GUI.skin.label.fontSize = proper_fontsize + 10;
        GUI.skin.label.normal.textColor = new Vector4(0.05f, 0.05f, 0.0f, 1.0f);
        GUI.Label(new Rect(Screen.width / 2 - 280, Screen.height - 50 - bar_height, 330f, 100), "Black : " + blackscore);
        GUI.skin.label.normal.textColor = new Vector4(0.95f, 0.95f, 1.0f, 1.0f);
        GUI.Label(new Rect(Screen.width / 2 + 150, Screen.height - 50 - bar_height, 330f, 100), "White : " + whitescore);
        GUI.Label(new Rect(Screen.width / 4 * 3 + 50, Screen.height / 16, Screen.width / 4 - 50, Screen.height / 8 * 7f), output_str);
        GUI.skin.label.fontSize = proper_fontsize;

        if (gameover != 0)
        {
            GUI.skin.label.fontSize = proper_fontsize + 20;
            if (gameover == 1)
            {
                GUI.skin.label.normal.textColor = new Vector4(0.15f, 0.15f, 0.15f, 1.0f);
                GUI.Label(new Rect(Screen.width / 5 * 4, Screen.height / 2, Screen.width / 4, Screen.height / 3 * 2), "Black win");
            }
            else if (gameover == -1)
            {
                GUI.skin.label.normal.textColor = new Vector4(0.98f, 0.98f, 0.98f, 1.0f);
                GUI.Label(new Rect(Screen.width / 5 * 4, Screen.height / 2, Screen.width / 4, Screen.height / 3 * 2), "White win");
            }
            else
            {
                GUI.skin.label.normal.textColor = new Vector4(0.98f, 0.98f, 0.98f, 1.0f);
                GUI.Label(new Rect(Screen.width / 5 * 4, Screen.height / 2, Screen.width / 4, Screen.height / 3 * 2), "Noun");
            }
            GUI.skin.label.fontSize = proper_fontsize;
        }

        if (((GUI.Button(new Rect(0, big_button_size * 3, big_button_size, big_button_size), btnback, gooder) || Input.GetKeyDown(KeyCode.Space)) && dosander > 10) && is_my_turn == 0 && poping_history == 0)
        {
            dosander = 0;
            whowin = 0;
            poping_history = 40;
        }

        if ((GUI.Button(new Rect(0, big_button_size * 4.2f, big_button_size * 1.2f, 50), "更换棋子", gooder) || Input.GetKeyDown(KeyCode.Space)) && dosander > 10)
        {
            tropical_side *= -1;
            if (tropical_side == -1)
                filer.WriteTextFile("turn.txt", "white");
            else
            {
                filer.WriteTextFile("turn.txt", "black");
            }
            initboard();
            recev.SendMessage("Restartnow", 1, SendMessageOptions.DontRequireReceiver);
        }
    }

    //---------------------- Core ---------------------------------------------------------------
    Stack<Vector2Int>[,] basic_valid_map(int[,] chessboard_t, int color)
    {
        Stack<Vector2Int>[,] res = new Stack<Vector2Int>[8, 8];
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                res[i, j] = new Stack<Vector2Int>();
            }
        int possible_num = 0;
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (chessboard_t[i, j] == 0)
                {
                    Stack<Vector2Int> thc = could_flip_at(chessboard_t, i, j, color);
                    if (thc.Count > 0)
                    {
                        res[i, j] = thc;
                        flip_possible_map[i, j] = true;
                        possible_num++;
                    }
                }
            }
        }
        return res;
    }

    int specialmap_score(int posx, int posy)
    {
        if ((posx == 0 || posx == 7) && (posy == 0 || posy == 7))
        {
            return 10000000;
        }
        if ((posx == 1 || posx == 6) && (posy == 1 || posy == 6))
        {
            return -10000;
        }
        if (((posx == 0 || posx == 7) && (posy == 1 || posy == 6)) || ((posx == 1 || posx == 6) && (posy == 0 || posy == 7)))
        {
            return -3000;
        }
        return 0;
    }
    int score_analysis(int[,] board, int color, int[] scale, int tower = 3)
    {
        if (tower <= 0)
            return 0;

        int[,] cpboard = new int[8, 8];
        copyboard(cpboard, board);
        Stack<Vector2Int>[,] nowpossible = basic_valid_map(cpboard, color);
        bool[,] ispossible = new bool[8, 8];
        int total_possible_number = 0;
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                if (nowpossible[i, j].Count > 0)
                {
                    total_possible_number++;
                    ispossible[i, j] = true;
                }
            }
        int maxscore = -1000000;
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                if (ispossible[i, j])
                {
                    int nowscore = specialmap_score(i, j);
                    int eatscore = nowpossible[i, j].Count * scale[0];
                    nowscore += eatscore;

                    cpboard[i, j] = color;
                    foreach (Vector2Int aa in nowpossible[i, j])
                    {
                        cpboard[aa.x, aa.y] *= -1;
                    }
                    // imagine area

                    int enemymov_ablity = calculate_move_ability(cpboard, -color);
                    int mymov_ability = calculate_move_ability(cpboard, color);
                    int cut_score = (-enemymov_ablity + mymov_ability) * scale[1];

                    nowscore += cut_score;

                    // int fight_back_ablity = calculate_fight_back_move_ablility(cpboard, -color);
                    // nowscore += fight_back_ablity * scale[2];
                    if (cut_score >= 0)
                    {
                        int nextscore = score_analysis(cpboard, -color, scale, tower - 1);
                        nowscore -= nextscore;
                        nowscore += 20000; // 活力高奖励
                    }


                    // end imagine
                    cpboard[i, j] = 0;
                    foreach (Vector2Int aa in nowpossible[i, j])
                    {
                        cpboard[aa.x, aa.y] *= -1;
                    }
                    if (nowscore > maxscore)
                    {
                        maxscore = nowscore;
                    }
                }
            }
        return maxscore;
    }
    bool analysis_board(int[,] board, int color, int[] scale)
    {
        Vector2Int nextstep = new Vector2Int(-1, -1);
        int[,] cpboard = new int[8, 8];
        copyboard(cpboard, board);
        Stack<Vector2Int>[,] nowpossible = basic_valid_map(cpboard, color);
        bool[,] ispossible = new bool[8, 8];
        nowpossible_global = nowpossible;
        cpboard_global = cpboard;
        bool havepos = false;
        for (int i = 0; i < scale.Length; i++)
        {
            scale_global[i] = scale[i];
        }

        int total_possible_number = 0;
        wait_num_global = 0;
        current_return_global = 0;
        output_str = "";
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                score_map_global[i, j] = -1000000000;
                if (nowpossible[i, j].Count > 0)
                {
                    total_possible_number++;
                    ispossible[i, j] = true;
                    wait_num_global++;
                }
            }
        int maxscore = -100000000;
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                if (ispossible[i, j])
                {
                    havepos = true;

                    Thread thr = new Thread(Thread_score_analysis);
                    thread_actnow.Push(thr);
                    //启动线程,传入参数
                    thr.Start("" + i + "," + j);

                }
            }
        return havepos;
    }
    void Thread_score_analysis(object data)
    {
        string recev = (string)data;
        int sx = recev[0] - '0';
        int sy = recev[2] - '0';
        int nowscore = center_scorer(sx, sy, nowpossible_global[sx, sy], cpboard_global, nextcolor, scale_global);
        output_str += "( " + sx + "," + sy + " ) : " + nowscore + "\n";
        score_map_global[sx, sy] = nowscore;
        current_return_global++;
        // print("fin search " + sx + "," + sy);
    }
    int center_scorer(int i, int j, Stack<Vector2Int> possiblepoint, int[,] cpboard_out, int color, int[] scale)
    {
        int[,] cpboard = new int[8, 8];
        copyboard(cpboard, cpboard_out);

        int nowscore = specialmap_score(i, j);
        int eatscore = possiblepoint.Count * scale[0];
        nowscore += eatscore;

        cpboard[i, j] = color;
        foreach (Vector2Int aa in possiblepoint)
        {
            cpboard[aa.x, aa.y] *= -1;
        }
        // imagine area

        int enemymov_ablity = calculate_move_ability(cpboard, -color);
        int cut_score = (5 - enemymov_ablity) * scale[1];
        nowscore += cut_score;

        int fight_back_ablity = calculate_fight_back_move_ablility(cpboard, -color);
        nowscore += fight_back_ablity * scale[2];

        if (nowscore > 4000)
            nowscore -= score_analysis(cpboard, -color, scale, 8);
        else
        {
            nowscore -= score_analysis(cpboard, -color, scale, 8);
        }
        //nowscore += Random.Range(0, 400);
        // end imagine
        cpboard[i, j] = 0;
        foreach (Vector2Int aa in possiblepoint)
        {
            cpboard[aa.x, aa.y] *= -1;
        }
        return nowscore;
    }
    int calculate_move_ability(int[,] board, int color)
    {
        int res = 0;
        Stack<Vector2Int>[,] nowpossible = basic_valid_map(board, color);
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                if (nowpossible[i, j].Count > 0)
                {
                    res++;
                    if ((i == 0 || i == 7) && (j == 0 || j == 7))
                    {
                        res += 100;
                    }
                    if ((i == 1 || i == 6) && (j == 1 || j == 6))
                    {
                        res -= 1;
                    }
                }

            }
        return res;
    }

    int calculate_fight_back_move_ablility(int[,] board, int color)
    {
        int res = 100;
        Vector2Int nextstep = new Vector2Int(-1, -1);
        int[,] cpboard = new int[8, 8];
        copyboard(cpboard, board);
        Stack<Vector2Int>[,] nowpossible = basic_valid_map(cpboard, color);
        bool[,] ispossible = new bool[8, 8];
        int total_possible_number = 0;
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                if (nowpossible[i, j].Count > 0)
                {
                    total_possible_number++;
                    ispossible[i, j] = true;
                }
            }
        int maxscore = -1000000;
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                if (ispossible[i, j])
                {
                    cpboard[i, j] = color;
                    foreach (Vector2Int aa in nowpossible[i, j])
                    {
                        cpboard[aa.x, aa.y] *= -1;
                    }
                    // imagine area

                    int enemymov_ablity = calculate_move_ability(cpboard, -color);
                    if (enemymov_ablity < res)
                    {
                        res = enemymov_ablity;
                    }

                    // end imagine
                    cpboard[i, j] = 0;
                    foreach (Vector2Int aa in nowpossible[i, j])
                    {
                        cpboard[aa.x, aa.y] *= -1;
                    }
                }
            }
        return res;
    }
}
