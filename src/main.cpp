#include "core/application.h"


int main()
{
	// Create the application
	Application* app = new Application();


	// Init must be called last as this fires off the rest of
	// the GL stuff. Meaning our UI objects and the like must be added 
	app->Init();
}