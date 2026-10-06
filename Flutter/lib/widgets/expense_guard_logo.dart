import 'package:flutter/material.dart';

class ExpenseGuardLogo extends StatelessWidget {
  const ExpenseGuardLogo({super.key, this.size = 40});
  final double size;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      label: 'ExpenseGuard',
      image: true,
      child: CustomPaint(
        size: Size.square(size),
        painter: const _LogoPainter(),
      ),
    );
  }
}

class _LogoPainter extends CustomPainter {
  const _LogoPainter();

  @override
  void paint(Canvas canvas, Size size) {
    final scale = size.width / 40;
    canvas.scale(scale);
    final rect = RRect.fromRectAndRadius(const Rect.fromLTWH(0, 0, 40, 40), const Radius.circular(10));
    canvas.drawRRect(
      rect,
      Paint()
        ..shader = const LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [Color(0xFF6366F1), Color(0xFF8B5CF6)],
        ).createShader(const Rect.fromLTWH(0, 0, 40, 40)),
    );

    final shield = Path()
      ..moveTo(20, 7.6)
      ..cubicTo(25.2, 8.6, 28.6, 9.2, 28.6, 9.2)
      ..cubicTo(29.2, 9.3, 29.6, 9.8, 29.6, 10.4)
      ..lineTo(29.6, 20)
      ..cubicTo(29.6, 26.6, 25.1, 31.2, 20.5, 33.4)
      ..cubicTo(20.2, 33.5, 19.8, 33.5, 19.5, 33.4)
      ..cubicTo(14.9, 31.2, 10.4, 26.6, 10.4, 20)
      ..lineTo(10.4, 10.4)
      ..cubicTo(10.4, 9.8, 10.8, 9.3, 11.4, 9.2)
      ..cubicTo(14.8, 8.6, 19.5, 7.6, 20, 7.6)
      ..close();
    canvas.drawPath(shield, Paint()..color = Colors.white);

    canvas.drawPath(
      Path()
        ..moveTo(15.2, 19.7)
        ..lineTo(18.6, 23)
        ..lineTo(25, 16.2),
      Paint()
        ..color = const Color(0xFF6366F1)
        ..style = PaintingStyle.stroke
        ..strokeWidth = 2.4
        ..strokeCap = StrokeCap.round
        ..strokeJoin = StrokeJoin.round,
    );
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}
