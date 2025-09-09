import re
import math

variables = {}

def calculate(expression):
    """
    Evaluates a mathematical expression with scientific functions and variables.
    """
    # Sanitize the expression to allow numbers, operators, and functions
    allowed_chars = r"^[0-9\.\+\-\*\/\(\)\s,a-zA-Z_]+$"
    if not re.match(allowed_chars, expression):
        raise ValueError("Invalid characters in expression")

    # Create a dictionary of safe functions from the math module
    safe_dict = {
        "sin": math.sin,
        "cos": math.cos,
        "tan": math.tan,
        "log": math.log,
        "log10": math.log10,
        "sqrt": math.sqrt,
        "pi": math.pi,
        "e": math.e,
    }

    # Add variables to the safe dictionary
    safe_dict.update(variables)

    try:
        # Evaluate the expression in a controlled environment
        result = eval(expression, {"__builtins__": None}, safe_dict)
        return result
    except (SyntaxError, ZeroDivisionError, NameError, TypeError, ValueError) as e:
        return f"Error: {e}"

def main():
    """
    Main function for the calculator.
    """
    print("Welcome to the command-line calculator!")
    print("Supported functions: sin, cos, tan, log, log10, sqrt")
    print("You can also use variables, e.g., x = 10")
    print("Enter 'exit' or 'quit' to end the program.")
    while True:
        try:
            expression = input("> ")
            if expression.lower() in ["exit", "quit"]:
                break

            # Check for variable assignment
            match = re.match(r"^\s*([a-zA-Z_][a-zA-Z0-9_]*)\s*=\s*(.*)$", expression)
            if match:
                var_name = match.group(1)
                value_expr = match.group(2)
                value = calculate(value_expr)
                if isinstance(value, (int, float)):
                    variables[var_name] = value
                    print(f"{var_name} = {value}")
                else:
                    print(f"Error: Invalid value for variable '{var_name}'")
            else:
                result = calculate(expression)
                print(result)
        except (ValueError, EOFError) as e:
            print(f"Error: {e}")
            break

if __name__ == "__main__":
    main()
