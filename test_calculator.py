import unittest
import calculator

class TestCalculator(unittest.TestCase):

    def test_basic_arithmetic(self):
        self.assertEqual(calculator.calculate("2 + 2"), 4)
        self.assertEqual(calculator.calculate("10 - 5"), 5)
        self.assertEqual(calculator.calculate("3 * 4"), 12)
        self.assertEqual(calculator.calculate("10 / 2"), 5)
        self.assertEqual(calculator.calculate("(2 + 3) * 4"), 20)

    def test_scientific_functions(self):
        self.assertAlmostEqual(calculator.calculate("sin(0)"), 0)
        self.assertAlmostEqual(calculator.calculate("cos(0)"), 1)
        self.assertAlmostEqual(calculator.calculate("tan(0)"), 0)
        self.assertAlmostEqual(calculator.calculate("log(1)"), 0)
        self.assertAlmostEqual(calculator.calculate("log10(10)"), 1)
        self.assertAlmostEqual(calculator.calculate("sqrt(4)"), 2)

    def test_variables(self):
        calculator.variables["x"] = 10
        self.assertEqual(calculator.calculate("x * 2"), 20)
        calculator.variables["y"] = 5
        self.assertEqual(calculator.calculate("x + y"), 15)

    def test_invalid_input(self):
        with self.assertRaises(ValueError):
            calculator.calculate("2 & 3")
        self.assertIn("Error", calculator.calculate("1 / 0"))
        self.assertIn("Error", calculator.calculate("log(-1)"))

if __name__ == "__main__":
    unittest.main()
