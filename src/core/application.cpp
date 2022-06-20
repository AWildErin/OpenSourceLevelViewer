#include "application.h"
#include <iostream>

Application::Application()
{
}

Application::~Application()
{
}

void Application::Init()
{
    if (!glfwInit())
    {
        std::cerr << "Error: GLFW failed to initialise!" << std::endl;
        exit(EXIT_FAILURE);
    }

    //glfwSetErrorCallback(error_callback);
    glfwWindowHint(GLFW_CONTEXT_VERSION_MAJOR, 2);
    glfwWindowHint(GLFW_CONTEXT_VERSION_MINOR, 0);
    pWindow = glfwCreateWindow(640, 480, "Open Source Level Viewer", nullptr, nullptr);

    if (!pWindow)
    {
        std::cerr << "Error: Window creation failed" << std::endl;
        this->Shutdown(EXIT_FAILURE);
    }

    glfwMakeContextCurrent(pWindow);
    //glfwSetKeyCallback(pWindow, key_callback);

    int width, height;
    glfwGetFramebufferSize(pWindow, &width, &height);
    glViewport(0, 0, width, height);
    glfwSetFramebufferSizeCallback(pWindow, Callback_FrameBufferSize);
    glClearColor(0.0f, 0.0f, 0.0f, 1.0f);
    glfwSwapInterval(1);

    // Init all our managers
    for (Manager* man : pManagers)
    {
        man->Initialise();
    }

    while (!glfwWindowShouldClose(pWindow))
    {
        glClear(GL_COLOR_BUFFER_BIT);

        this->Render();

        glfwSwapBuffers(pWindow);
        glfwPollEvents();
    }

    // Shutdown and delete all our managers
    for (Manager* man : pManagers)
    {
        man->Shutdown();
        delete man;
    }

    this->Shutdown(EXIT_SUCCESS);
}

void Application::Shutdown(int exitType)
{
    glfwDestroyWindow(pWindow);
    glfwTerminate();
    exit(exitType);
}

// todo: Actually check if the manager was already added
bool Application::AddManager(Manager* pManager)
{
    pManagers.push_back(pManager);
    return true;
}

bool Application::RemoveManager(Manager* pManager)
{
    return true;
}

void Application::Render()
{
    // Render all our managers
    for (Manager* man : pManagers)
    {
        man->Render();
    }
}

void Application::Callback_FrameBufferSize(GLFWwindow* window, int width, int height)
{
    glViewport(0, 0, width, height);
}