Please create a file "SudokuWebGame.prd.md" in the plans folder at the root of this repository.

This document should outline requirements for an ASP.NET Core/Vue based web application that allows users to play Sudoku online.

The document should include the following sections:
1. **Overview**: A brief description of the application and its purpose.
2. **Features**: A detailed list of features the application should have (see below for more details on features)
3. **User Interface**: A description of the user interface and user experience design.
4. **Technical Requirements**: A list of technical requirements and constraints for the application.
5. **Testing and Quality Assurance**: A plan for testing the application to ensure it meets the requirements and is free of bugs.

## Features to include:
- Sudoku puzzle generation with varying difficulty levels (easy, medium, hard).
- Timer and scoring system to track user performance.
- Highlighting of conflicting numbers to assist users in identifying mistakes.
- "Scratch" mode that allows users to make notes in the cells for potential numbers.
- Optional hints and solution reveal features for users who need assistance.
- User authentication and profile management.
    - Authentication should be implemented using social login options with Google, GitHub, and Facebook login all supported.
    - Profile information should include username, email, and an optional profile picture.
- A leaderboard to display top players based on their scores and completion times.
    - Hints and solution reveal features will disqualify users from the leaderboard.

## User Interface:
- The application should have a clean and intuitive interface that is easy to navigate.
- The `frontend-design` skill must be used to create a visually appealing design that enhances the user experience.
- The Sudoku grid should be prominently displayed, with clear indicators for selected cells and any conflicts.
- The timer and scoring system should be easily visible to the user.
- The application should be responsive and work well on both desktop and mobile devices.
- The user profile and leaderboard should be accessible from the main menu.
- The plan should specify a color scheme and design elements that align with the theme of the application, ensuring a cohesive and engaging user experience.

## Technical Requirements:
- The backend should be developed using ASP.NET Core, and the frontend should be built with Vue.js.
    - All standard best practices for both frameworks should be followed, including proper separation of concerns, use of components, and adherence to RESTful API design principles.
- The solution should use an Aspire host for deployment. Use the `aspire` skill to manage Aspire-related work.
- The application should use a relational database (e.g., SQL Server, PostgreSQL) to store user data, game states, and leaderboard information.
    - The application should use an ORM (e.g., Entity Framework Core) for database interactions.

## Test Requirements:
- The plan should include a comprehensive testing strategy that covers unit testing, integration testing, and end-to-end testing.
    - The plan should include specific areas to focus on for testing, such as the puzzle generation algorithm, user authentication, and the scoring system.
    - Unit tests should be written for both the backend and frontend components to ensure that individual functions and components work as expected.
    - Integration tests should be implemented to verify that different parts of the application work together correctly.
    - End-to-end tests should be conducted to simulate user interactions and ensure that the application functions correctly from the user's perspective.
    - All solution components should have 80% or higher code coverage (between unit tests and integration tests) to ensure a high level of confidence in the application's correctness.
- While implementing the plan, the `playwright-cli` skill should be used to automate end-to-end testing of the application, ensuring that all features work as intended.

## Inputs:
- Plan generation should use this prompt as the primary input.
- You should ask the user for clarifications or for additional information, as needed, to ensure that the plan is comprehensive and ready to be implemented by a development team.

## Outputs:
- A well-structured Product Requirements Document (PRD) in markdown format that can be used by an LLM to build the Sudoku web application.
- The PRD should be created at `/plans/SudokuWebGame.prd.md` in the repository root.
