You are an expert Senior Angular Engineer specializing in Angular Material.

**Task:**
Create the main scaffolding layout component for an Angular application

**Design Structural Spec (Markdown):**

# Main Layout Structure (Flex Column, 100vh height)

## 1. Top Bar (MatToolbar - Primary color, sticky top)
- Menu Button (MatIconButton, Icon: 'menu') -> Toggles the Sidenav signal
- Title Text: "MVP Finance"
- Spacer (CSS flex-grow)

## 2. Sidenav Container (MatSidenavContainer - flex-grow to fill remaining space)

### A. The Sidebar (MatSidenav)
- **Settings:** Mode="side", Opened="true" (bound to signal)
- **Content:**
    - MatNavList
        - Link: Home (Icon: home)
        - Link: Settings (Icon: settings)

### B. The Main Content Area (MatSidenavContent - flex column)
- **Body wrapper:** (flex-grow, padding: 20px)
    - `<router-outlet></router-outlet>` (Placeholder for page content)

**Output Requirements:**
Please provide the full code for:
1.  `app.ts` (Include all necessary imports like `MatSidenavModule`, `MatToolbarModule`, `RouterModule`, etc., and the signal logic).
2.  `app.html` (The template matching the spec).
3.  `app.scss` (The CSS Flexbox styles to ensure the toolbar stays top, footer stays bottom, and the middle stretches).